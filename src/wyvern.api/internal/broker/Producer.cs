using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using Akka;
using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Pattern;
using Akka.Persistence.Query;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.Streams.Util;
using Amqp;
using Amqp.Framing;
using Amqp.Sasl;
using Newtonsoft.Json;
using wyvern.api.abstractions;
using wyvern.api.@internal.readside;
using wyvern.api.@internal.surfaces;
using wyvern.entity.@event.aggregate;
using wyvern.utils;
using static Producer;
using static wyvern.api.@internal.readside.ClusterDistributionExtensionProvider;

internal static partial class Producer
{
    public static void StartTaggedOffsetProducer<TMessage>(
            ActorSystem system,
            ImmutableArray<AggregateEventTag> tags,
            TopicConfig topicConfig,
            string topicId,
            Func<string, Offset, Source<KeyValuePair<TMessage, Offset>, NotUsed>> eventStreamFactory,
            IOffsetStore offsetStore
        )
    {
        var producerConfig = new ProducerConfig(system.Settings.Config);
        var publisherProps = TaggedOffsetProducerActor.Props<TMessage>(
            topicConfig, topicId, eventStreamFactory, offsetStore
        );

        var backoffPublisherProps = BackoffSupervisor.PropsWithSupervisorStrategy(
            publisherProps,
            "producer",
            producerConfig.MinBackoff,
            producerConfig.MaxBackoff,
            producerConfig.RandomBackoffFactor,
            SupervisorStrategy.StoppingStrategy
        );

        var clusterShardingSettings = ClusterShardingSettings
            .Create(system)
            .WithRole(producerConfig.Role);

        new ClusterDistribution(system)
            .Start(
                $"serviceBusProducer-{topicId}",
                backoffPublisherProps,
                tags.Select(x => x.Tag).ToArray(),
                new ClusterDistributionSettings(system, clusterShardingSettings)
            );
    }

    public static class TaggedOffsetProducerActor
    {
        public static Props Props<TMessage>(
                            TopicConfig topicConfig,
                            string topicId,
                            Func<string, Offset, Source<KeyValuePair<TMessage, Offset>, NotUsed>> eventStreamFactory,
                            IOffsetStore offsetStore
                        )
        {
            return Akka.Actor.Props.Create(() =>
                new TaggedOffsetProducerActor<TMessage>(
                    topicConfig,
                    topicId,
                    eventStreamFactory,
                    offsetStore
                )
            );
        }
    }

    public class TaggedOffsetProducerActor<TMessage> : ReceiveActor
    {
        private Option<IKillSwitch> Shutdown = Option<IKillSwitch>.None;

        TopicConfig TopicConfig { get; }
        string TopicId { get; }
        Func<string, Offset, Source<KeyValuePair<TMessage, Offset>, NotUsed>> EventStreamFactory { get; }
        IOffsetStore OffsetStore { get; }

        Connection Connection { get; }

        public TaggedOffsetProducerActor(
            TopicConfig topicConfig,
            string topicId,
            Func<string, Offset, Source<KeyValuePair<TMessage, Offset>, NotUsed>> eventStreamFactory,
            IOffsetStore offsetStore
        )
        {
            TopicConfig = topicConfig;
            TopicId = topicId;
            EventStreamFactory = eventStreamFactory;
            OffsetStore = offsetStore;

            Connection = new Connection(
                new Amqp.Address(TopicConfig.Endpoint.Value),
                SaslProfile.Anonymous,
                new Open()
                {
                    ContainerId = "client.1.2",
                    HostName = "localhost",
                    MaxFrameSize = 8 * 1024
                },
                (c, o) => { });

            Receive<EnsureActive>(ensureActive =>
            {
                var daoFuture = OffsetStore.Prepare($"topicProducer-{TopicId}", ensureActive.EntityId);
                daoFuture.PipeTo(Self);
                Become(() => Initializing(ensureActive.EntityId));
            });
        }

        protected override void PostStop()
        {
            Context.System.Log.Info($"Shutting down topicProducer-{TopicId}");
            Shutdown.ForEach(x => x.Shutdown());
        }

        private void GeneralHandler()
        {
            Receive<EnsureActive>(ensureActive => { });
            Receive<Status.Failure>(e => throw e.Cause);
        }

        private void Initializing(string entityId)
        {
            GeneralHandler();

            var endpoint = TopicConfig.Endpoint;
            Receive<EnsureActive>(ensureActive => { });
            Receive<IOffsetDao>(offsetDao =>
            {
                if (endpoint.HasValue)
                {
                    Run(entityId as string, endpoint.Value, offsetDao);
                }
                else
                {
                    Context.System.Log.Error($"Failed to configure endpoint for producer [{entityId}]");
                    Context.Stop(Self);
                }
            });

            // receive none, context.stop

        }

        public void Active()
        {
            GeneralHandler();
            Receive<Done>(_ =>
            {
                Context.System.Log.Info($"Service bus producer stream for topic {TopicId} is complete.");
                Context.Stop(Self);
            });
        }

        public void Run(string tag, string endpoint, IOffsetDao offsetDao)
        {
            // TODO: Use Offset DAO
            EventStreamFactory.Invoke(tag, Offset.NoOffset())
                    .ViaMaterialized(KillSwitches.Single<KeyValuePair<TMessage, Offset>>(), Keep.Right)
                    .Via(EventsPublisherFlow(endpoint, offsetDao))
                    .ToMaterialized(Sink.Ignore<Task<Done>>(), Keep.Both)
                    .Run(Context.Materializer());

            Become(() => Active());
        }

        private IGraph<FlowShape<KeyValuePair<TMessage, Offset>, Task<Done>>, NotUsed> EventsPublisherFlow(string endpoint, IOffsetDao offsetDao)
        {
            return Flow.FromGraph(
                GraphDsl.Create(
                    Flow.FromFunction(
                        (TMessage m) => ServiceBusFlowPublisher(endpoint, m)
                    ),
                    (builder, shape) =>
                    {
                        var unzip = builder.Add(new UnZip<TMessage, Offset>());
                        var zip = builder.Add(new Zip<Task<Done>, Offset>());
                        var offsetCommitter = builder.Add(
                            Flow.FromFunction(
                                (Tuple<Task<Done>, Offset> x) =>
                                    offsetDao.SaveOffset(x.Item2)
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

        private Task<Done> ServiceBusFlowPublisher(string endpoint, TMessage m)
        {
            try
            {
                var session = new Session(Connection);
                var senderLink = new SenderLink(session, "session", "queue");

                var bf = new BinaryFormatter();
                using (var ms = new MemoryStream())
                {
                    var obj = JsonConvert.SerializeObject(m);
                    bf.Serialize(ms, obj);

                    senderLink.Send(new Message
                    {
                        BodySection = new Data
                        {
                            Binary = ms.ToArray()
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Context.System.Log.Error(ex, ex.Message);
            }

            return Task.FromResult(Done.Instance);
        }

    }


}
