using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Akka;
using Akka.Actor;
using Akka.Persistence.Query;
using Akka.Streams;
using Akka.Streams.Dsl;
using wyvern.entity.@event;
using wyvern.entity.@event.aggregate;

namespace wyvern.api.@internal.surfaces
{
    public interface IOffsetStore
    {
        Offset LoadedOffset { get; }
        Task<Done> SaveOffset(Offset o);
    }

    public class ServiceBusFlowPublisher
    {

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
        }

        public Func<AggregateEventTag, Offset, Source<KeyValuePair<TMessage, Offset>, NotUsed>> ReadSideStream { get; }
        public ImmutableArray<AggregateEventTag> Tags { get; }

        public Flow<TMessage, Task<Done>, NotUsed> ServiceBusFlowPublisher(string endpoints)
        {
            return Flow.FromFunction<TMessage, Task<Done>>(message =>
            {
                Console.WriteLine(message);
                return Task.FromResult(Done.Instance);
            });
        }

        public Flow<KeyValuePair<TMessage, Offset>, Tuple<TMessage, Offset>, NotUsed> EventsPublisherFlow(string endpoints, IOffsetStore offsetsDao)
        {
            return Flow.FromGraph(
                GraphDsl.Create(
                    ServiceBusFlowPublisher(endpoints),
                    (builder, shape) =>
                    {
                        var unzip = builder.Add(new UnZip<TMessage, Offset>());
                        var zip = builder.Add(new Zip<TMessage, Offset>());

                        var offsetCommitter = builder.Add(
                            Flow.FromFunction(
                                (Tuple<TMessage, Offset> e) =>
                                {
                                    offsetsDao.SaveOffset(e.Item2);
                                    return KeyValuePair.Create(e.Item1, e.Item2);
                                })
                        );

                        builder.From(unzip.Out0).To(zip.In0);
                        builder.From(unzip.Out1).To(zip.In1);
                        builder.From(zip.Out).To(offsetCommitter.Inlet);

                        return new FlowShape<KeyValuePair<TMessage, Offset>, Tuple<TMessage, Offset>>(unzip.In, zip.Out);
                    }
                )
            );
        }

        public void Init(ActorSystem sys)
        {
            IOffsetStore offsetDao = null; // TODO: need dao impl
            foreach (var tag in Tags)
            {
                var str = ReadSideStream.Invoke(tag, Offset.NoOffset());

                str.ViaMaterialized(KillSwitches.Single<KeyValuePair<TMessage, Offset>>(), Keep.Right)
                //.Via(EventsPublisherFlow("endpoint", offsetDao));
                .ToMaterialized(Sink.Ignore<KeyValuePair<TMessage, Offset>>(), Keep.Both)
                .Run(ActorMaterializer.Create(sys));
            }
        }
    }
}
