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
using wyvern.entity.@event;
using wyvern.entity.@event.aggregate;
using wyvern.utils;
using static Producer;
using static wyvern.api.@internal.readside.ClusterDistributionExtensionProvider;

internal static partial class Producer
{
    /// <summary>
    /// Static helper class for creating props
    /// </summary>
    public static class TaggedOffsetProducerActor
    {
        public static Props Props<TEvent>(
            TopicConfig topicConfig,
            string topicId,
            Func<string, Offset, Source<KeyValuePair<TEvent, Offset>, NotUsed>> eventStreamFactory,
            ISerializer serializer,
            IMessageExtractor extractor,
            IOffsetStore offsetStore
        ) where TEvent : AbstractEvent
        {
            return Akka.Actor.Props.Create(() =>
                new TaggedOffsetProducerActor<TEvent>(
                    topicConfig,
                    topicId,
                    eventStreamFactory,
                    serializer,
                    extractor,
                    offsetStore
                )
            );
        }
    }

    /// <summary>
    /// Start a Tagged Offset Producer over a cluster distribution
    /// </summary>
    /// <param name="system"></param>
    /// <param name="tags"></param>
    /// <param name="topicConfig"></param>
    /// <param name="topicId"></param>
    /// <param name="Func<string"></param>
    /// <param name="eventStreamFactory"></param>
    /// <param name="offsetStore"></param>
    /// <typeparam name="TMessage"></typeparam>
    public static void StartTaggedOffsetProducer<TEvent>(
        ActorSystem system,
        ImmutableArray<AggregateEventTag> tags,
        TopicConfig topicConfig,
        string topicId,
        Func<string, Offset, Source<KeyValuePair<TEvent, Offset>, NotUsed>> eventStreamFactory,
        ISerializer serializer,
        IOffsetStore offsetStore
    ) where TEvent : AbstractEvent
    {
        var producerConfig = new ProducerConfig(system.Settings.Config);
        var publisherProps = TaggedOffsetProducerActor.Props<TEvent>(
            topicConfig, topicId, eventStreamFactory, serializer, offsetStore
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

        // TODO: how to make this a singleton so we don't have to join self

        new ClusterDistribution(system)
            .Start(
                $"serviceBusProducer-{topicId}",
                backoffPublisherProps,
                tags.Select(x => x.Tag).ToArray(),
                new ClusterDistributionSettings(system, clusterShardingSettings)
            );

    }

    /// <summary>
    /// Tagged offset producer actor
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public class TaggedOffsetProducerActor<TEvent> : ReceiveActor
    where TEvent : AbstractEvent
    {
        /// <summary>
        /// Coordinated shutdown
        /// </summary>
        private Option<IKillSwitch> Shutdown = Option<IKillSwitch>.None;

        /// <summary>
        /// Topic configuration
        /// </summary>
        /// <value></value>
        TopicConfig TopicConfig { get; }

        /// <summary>
        /// Topic identifier
        /// </summary>
        /// <value></value>
        string TopicId { get; }

        /// <summary>
        /// Event stream creation
        /// </summary>
        /// <value></value>
        Func<string, Offset, Source<KeyValuePair<TEvent, Offset>, NotUsed>> EventStreamFactory { get; }

        /// <summary>
        /// Serializer to use
        /// </summary>
        /// <value></value>
        ISerializer Serializer { get; }

        /// <summary>
        /// Extractor for message properties
        /// </summary>
        /// <value></value>
        IMessagePropertyExtractor Extractor { get; }

        /// <summary>
        /// Offset storage mechanism
        /// </summary>
        /// <value></value>
        IOffsetStore OffsetStore { get; }

        /// <summary>
        /// Service bus connection
        /// </summary>
        /// <value></value>
        Connection Connection { get; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="topicConfig"></param>
        /// <param name="topicId"></param>
        /// <param name="Func<string"></param>
        /// <param name="eventStreamFactory"></param>
        /// <param name="offsetStore"></param>
        public TaggedOffsetProducerActor(
            TopicConfig topicConfig,
            string topicId,
            Func<string, Offset, Source<KeyValuePair<TEvent, Offset>, NotUsed>> eventStreamFactory,
            ISerializer serializer,
            IMessagePropertyExtractor extractor,
            IOffsetStore offsetStore
        )
        {
            TopicConfig = topicConfig;
            TopicId = topicId;
            EventStreamFactory = eventStreamFactory;
            OffsetStore = offsetStore;
            Serializer = serializer;
            Extractor = extractor;
            Connection = new Connection(
                new Amqp.Address(
                    TopicConfig.Host.Value,
                    TopicConfig.UseAmqps.OrElse(false) ? 5671 : 5672,
                    TopicConfig.Username.OrElse(null),
                    TopicConfig.Password.OrElse(null),
                    "/",
                    TopicConfig.UseAmqps.OrElse(false) ? "AMQPS" : "AMQP"
                ));

            Receive<EnsureActive>(ensureActive =>
            {
                var daoFuture = OffsetStore.Prepare($"topicProducer-{TopicId}", ensureActive.EntityId);
                daoFuture.PipeTo(Self);
                Become(() => Initializing(ensureActive.EntityId));
            });

        }

        /// <summary>
        /// After stop, perform stop on killswitches
        /// </summary>
        protected override void PostStop()
        {
            Context.System.Log.Info($"Shutting down topicProducer-{TopicId}");
            Shutdown.ForEach(x => x.Shutdown());
        }

        /// <summary>
        /// Handle heartbeats and status failures
        /// </summary>
        private void GeneralHandler()
        {
            Receive<EnsureActive>(ensureActive => { });
            Receive<Status.Failure>(e =>
                throw e.Cause);
        }

        /// <summary>
        /// On initialize, receive the offsetdao and run the event stream.
        /// </summary>
        /// <param name="entityId"></param>
        private void Initializing(string entityId)
        {
            GeneralHandler();

            var endpoint = TopicConfig.Host;
            var entity = TopicConfig.Entity;

            Receive<IOffsetDao>(offsetDao =>
            {
                if (!endpoint.HasValue || !entity.HasValue)
                {
                    Context.System.Log.Error(
                        $"Failed to configure endpoint for producer [{entityId}].  Check the connection string parameters."
                    );
                    Context.Stop(Self);
                    return;
                }
                Run(entityId as string, TopicConfig, offsetDao);
            });

            // receive none, context.stop

        }

        /// <summary>
        /// Routine when actor is considered to be active.
        /// </summary>
        public void Active()
        {
            GeneralHandler();
            Receive<Done>(_ =>
            {
                Context.System.Log.Info($"Service bus producer stream for topic {TopicId} is complete.");
                Context.Stop(Self);
            });
        }

        /// <summary>
        /// Run the event stream
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="endpoint"></param>
        /// <param name="offsetDao"></param>
        public void Run(string tag, TopicConfig config, IOffsetDao offsetDao)
        {
            EventStreamFactory.Invoke(tag, offsetDao.LoadedOffset)
                .ViaMaterialized(KillSwitches.Single<KeyValuePair<TEvent, Offset>>(), Keep.Right)
                .Via(EventsPublisherFlow(config, offsetDao))
                .ToMaterialized(Sink.Ignore<Task<Done>>(), Keep.Both)
                .Run(Context.Materializer());

            Become(() => Active());
        }

        /// <summary>
        /// Create the event stream graph
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="offsetDao"></param>
        /// <returns></returns>
        private IGraph<FlowShape<KeyValuePair<TEvent, Offset>, Task<Done>>, NotUsed > EventsPublisherFlow(TopicConfig config, IOffsetDao offsetDao)
        {
            return Flow.FromGraph(
                GraphDsl.Create(
                    /* Publish */
                    Flow.FromFunction(
                        (TEvent m) => ServiceBusFlowPublisher(config, m)
                    ),
                    /* Unzip/Zip Flow */
                    (builder, shape) =>
                    {
                        var unzip = builder.Add(new UnZip<TEvent, Offset>());
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

                        return new FlowShape<KeyValuePair<TEvent, Offset>, Task<Done>>(
                            unzip.In, offsetCommitter.Outlet
                        );
                    }
                )
            );
        }

        /// <summary>
        /// Publish to service bus
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="m"></param>
        /// <returns></returns>
        private Task<Done> ServiceBusFlowPublisher(TopicConfig config, TEvent m)
        {
            try
            {
                var session = new Session(Connection);
                var senderLink = new SenderLink(session, "session", config.Entity.Value);

                var binary = Serializer.Serialize(m);
                var props = Extractor.Extract(m);

                var appProps = new Amqp.Framing.ApplicationProperties();
                foreach (var prop in props)
                    appProps[prop.Key] = prop.Value;

                senderLink.Send(new Message
                {
                    BodySection = new Data
                        {
                            Binary = binary
                        },
                        ApplicationProperties = appProps
                });

                senderLink.Close();
                session.Close();
            }
            catch (Exception ex)
            {
                Context.System.Log.Error(ex, ex.Message);
                throw;
            }

            return Task.FromResult(Done.Instance);
        }

    }

}