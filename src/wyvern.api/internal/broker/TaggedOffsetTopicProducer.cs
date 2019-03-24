using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using Akka;
using Akka.Actor;
using Akka.Persistence.Query;
using Akka.Streams;
using Akka.Streams.Dsl;
using Amqp;
using Amqp.Framing;
using Amqp.Sasl;
using Address = Amqp.Address;
using wyvern.entity.@event;
using wyvern.entity.@event.aggregate;
using Newtonsoft.Json;

namespace wyvern.api.@internal.surfaces
{
    public interface IOffsetStore
    {
        Offset LoadedOffset { get; }
        Task<Done> SaveOffset(Offset o);
    }

    // TODO: Proper OffsetStore implementation
    public class OffsetStore : IOffsetStore
    {
        public Offset LoadedOffset { get; private set; } = Offset.NoOffset();

        public Task<Done> SaveOffset(Offset o)
        {
            LoadedOffset = o;
            return Task.FromResult(Done.Instance);
        }
    }

    public interface InternalTopic
    {
    }

    public interface ITaggedOffsetTopicProducer<TMessage> : InternalTopic
        where TMessage : class
    {
        ImmutableArray<AggregateEventTag> Tags { get; }
        Func<AggregateEventTag, Offset, Source<KeyValuePair<TMessage, Offset>, NotUsed>> ReadSideStream { get; }
        void Init(ActorSystem sys);
    }

    public sealed class TaggedOffsetTopicProducer<TMessage> : InternalTopic<TMessage>, ITaggedOffsetTopicProducer<TMessage>
        where TMessage : class
    {
        public TaggedOffsetTopicProducer(
            ImmutableArray<AggregateEventTag> tags,
            Func<AggregateEventTag, Offset, Source<KeyValuePair<TMessage, Offset>, NotUsed>> readSideStream)
        {
            (Tags, ReadSideStream) = (tags, readSideStream);

            // TODO: Inject these.

            var connection = new Connection(
                new Address("amqp://guest:guest@localhost:5672"),
                SaslProfile.Anonymous,
                new Open()
                {
                    ContainerId = "client.1.2",
                    HostName = "localhost",
                    MaxFrameSize = 8 * 1024
                },
                (c, o) => { /* do someting with remote Open o */ });

            var session = new Session(connection);

            SenderLink = new SenderLink(session, "session", "q");
            OffsetStore = new OffsetStore();
        }

        public Func<AggregateEventTag, Offset, Source<KeyValuePair<TMessage, Offset>, NotUsed>> ReadSideStream { get; }
        public ImmutableArray<AggregateEventTag> Tags { get; }

        IOffsetStore OffsetStore { get; }
        SenderLink SenderLink { get; }

        public void Init(ActorSystem sys)
        {
            foreach (var tag in Tags)
            {
                ReadSideStream.Invoke(tag, Offset.NoOffset())
                    .ViaMaterialized(KillSwitches.Single<KeyValuePair<TMessage, Offset>>(), Keep.Right)
                    .Via(EventsPublisherFlow())
                    .ToMaterialized(Sink.Ignore<Task<Done>>(), Keep.Both)
                    .Run(sys.Materializer());
            }

        }


        private Task<Done> ServiceBusFlowPublisher(TMessage m)
        {
            // TODO: Partitionkey strategy

            var bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                var obj = JsonConvert.SerializeObject(m);
                bf.Serialize(ms, obj);

                SenderLink.Send(new Message
                {
                    BodySection = new Data
                    {
                        Binary = ms.ToArray()
                    }
                });
                return Task.FromResult(Done.Instance);
            }
        }

        private IGraph<FlowShape<KeyValuePair<TMessage, Offset>, Task<Done>>, NotUsed> EventsPublisherFlow()
        {
            return Flow.FromGraph(
                GraphDsl.Create(
                    Flow.FromFunction(
                        (TMessage m) => ServiceBusFlowPublisher(m)
                    ),
                    (builder, shape) =>
                    {
                        var unzip = builder.Add(new UnZip<TMessage, Offset>());
                        var zip = builder.Add(new Zip<Task<Done>, Offset>());
                        var offsetCommitter = builder.Add(
                            Flow.FromFunction(
                                (Tuple<Task<Done>, Offset> x) =>
                                    OffsetStore.SaveOffset(x.Item2)
                            )
                        );

                        builder.From(unzip.Out0).Via(shape).To(zip.In0);
                        builder.From(unzip.Out1).To(zip.In1);
                        builder.From(zip.Out).To(offsetCommitter.Inlet);

                        return new FlowShape<KeyValuePair<TMessage, Offset>, Task<Done>>(
                            unzip.In, offsetCommitter.Outlet
                        );
                    }
                )
            );
        }
    }
}
