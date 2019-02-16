using System;
using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Configuration;

namespace wyvern.api.@internal.readside
{
    internal class ClusterDistributionSettings
    {
        public ActorSystem ActorSystem { get; }
        public Config Config { get; }
        public ClusterShardingSettings ClusterShardingSettings { get; }
        public TimeSpan EnsureActiveInterval { get; }

        public ClusterDistributionSettings(
            ActorSystem system,
            ClusterShardingSettings clusterShardingSettings)
        {
            ActorSystem = system;
            ClusterShardingSettings = clusterShardingSettings;
            Config = ActorSystem.Settings.Config.GetConfig("wyvern.persistence.cluster.distribution");
            EnsureActiveInterval = Config.GetTimeSpan("ensure-active-interval");
        }
    }
}
