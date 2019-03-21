using System;
using System.Collections.Immutable;
using System.Linq;
using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Pattern;
using wyvern.api.@internal.readside;
using static Producer;
using static wyvern.api.@internal.readside.ClusterDistributionExtensionProvider;

internal static partial class Producer
{
    public class TaggedOffsetProducerActor<TM> : ReceiveActor
    {
        public TaggedOffsetProducerActor(
            TopicConfig topicConfig,
            string topicId,
            object eventStreamFactory,
            object offsetStore
        )
        {

        }
    }

    public static class TaggedOffsetProducerActor
    {

        public static Props Props<TM>(
            TopicConfig topicConfig,
            string topicId,
            object eventStreamFactory,
            object offsetStore
        )
        {
            return Akka.Actor.Props.Create(() =>
                new TaggedOffsetProducerActor<TM>(
                    topicConfig,
                    topicId,
                    eventStreamFactory,
                    offsetStore
                )
            );
        }
    }

    public class TopicConfig
    {

    }

    public static void StartTaggedOffsetProducer<TM>(
        ActorSystem system,
        ImmutableArray<string> tags,
        TopicConfig topicConfig,
        string topicId,
        object eventStreamFactory,
        object offsetStore
    )
    {
        var producerConfig = new ProducerConfig(system.Settings.Config);
        var publisherProps = TaggedOffsetProducerActor.Props<TM>(
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
                tags.ToArray(),
                new ClusterDistributionSettings(system, clusterShardingSettings)
            );
    }
}
