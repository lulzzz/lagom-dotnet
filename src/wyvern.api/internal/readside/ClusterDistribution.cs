using System;
using Akka.Actor;
using Akka.Cluster;
using Akka.Cluster.Sharding;
using Akka.Streams.Util;
using wyvern.utils;

// CHECKED
namespace wyvern.api.@internal.readside
{
    internal class ClusterDistributionExtensionProvider : ExtensionIdProvider<ClusterDistributionExtensionProvider.ClusterDistribution>, IExtensionId
    {
        private static int MaxShards { get; } = 1000;

        public sealed class EnsureActive
        {
            public string EntityId { get; }
            public EnsureActive(string entityId) => (EntityId) = (entityId);
        }

        public override ClusterDistribution CreateExtension(ExtendedActorSystem system)
            => new ClusterDistribution(system);

        internal class ClusterDistribution : IExtension
        {
            ActorSystem ActorSystem { get; }

            public ClusterDistribution(ActorSystem system)
            {
                ActorSystem = system;
            }

            public IActorRef Start(
                string typeName,
                Props entityProps,
                string[] entityIds,
                ClusterDistributionSettings settings)
            {

                ExtractEntityId extractEntityId = (msg) =>
                {
                    switch (msg)
                    {
                        case EnsureActive ensureActive:
                            return (ensureActive.EntityId, msg).ToTuple();
                        default:
                            return null;
                    }
                };
                ExtractShardId extractShardId = (msg) =>
                {
                    switch (msg)
                    {
                        case EnsureActive ensureActive:
                            if (entityIds.Length > MaxShards)
                                return Math.Abs(entityIds.Length % 1000).ToString();
                            return ensureActive.EntityId;
                        default:
                            return null;
                    }
                };

                var sharding = ClusterSharding.Get(ActorSystem);

                var role = String.IsNullOrEmpty(settings.ClusterShardingSettings.Role) ?
                    new Option<string>() : new Option<string>(settings.ClusterShardingSettings.Role);
                if (role.ForAll(Cluster.Get(ActorSystem).SelfRoles.Contains))
                {
                    var shardRegion = sharding.Start(
                        typeName,
                        entityProps,
                        settings.ClusterShardingSettings,
                        extractEntityId,
                        extractShardId
                    );

                    return ActorSystem.ActorOf(
                        EnsureActiveActor.Props(
                            entityIds,
                            shardRegion,
                            settings.EnsureActiveInterval,
                            // TODO: Url encode
                            "cluster-distribution-" + typeName
                            // TODO: why was name not local????
                        )
                    );
                }
                else
                {
                    return sharding.StartProxy(
                        typeName,
                        settings.ClusterShardingSettings.Role,
                        extractEntityId,
                        extractShardId
                    );
                }

            }

        }

    }
}
