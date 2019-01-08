using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Akka;
using Akka.Actor;
using Akka.Cluster;
using Akka.Cluster.Sharding;
using Akka.Persistence.Query;
using Akka.Persistence.Query.Sql;
using Akka.Persistence.SqlServer;
using Akka.Streams.Dsl;
using Akka.Streams.Util;
using wyvern.api.@internal.command;
using wyvern.api.ioc;
using wyvern.entity.command;
using wyvern.entity.@event;
using wyvern.entity.@event.aggregate;
using wyvern.entity.state;
using wyvern.utils;

namespace wyvern.api.@internal.sharding
{
    internal class ShardedEntityRegistry : IShardedEntityRegistry
    {
        /// <summary>
        ///     Registry for entities to be processed using the given actor system
        /// </summary>
        /// <param name="actorSystem"></param>
        public ShardedEntityRegistry(ActorSystem actorSystem)
        {
            ActorSystem = actorSystem;

            var persistenceConfig = ActorSystem.Settings.Config.GetConfig("wyvern.persistence");
            AskTimeout = persistenceConfig.GetTimeSpan("ask-timeout", allowInfinite: false);
            MaxNumberOfShards = Math.Max(persistenceConfig.GetInt("max-number-of-shards", 1), 1);
            var role = persistenceConfig.GetString("run-entities-on-role");
            Role = string.IsNullOrWhiteSpace(role) ? Option<string>.None : new Option<string>(role);
            SnapshotAfter = persistenceConfig.GetInt("snapshot-after", 10);

            Sharding = ClusterSharding.Get(ActorSystem);
            ShardingSettings = ClusterShardingSettings
                .Create(ActorSystem)
                .WithRole(Role.Value);

            QueryPluginId = new Option<string>(SqlReadJournal.Identifier);

            var persistence = SqlServerPersistence.Get(ActorSystem);
            EventsByTagQuery = PersistenceQuery.Get(ActorSystem)
                .ReadJournalFor<SqlReadJournal>(SqlReadJournal.Identifier);

            //var journalConfig = persistence.DefaultJournalConfig;
            //var sqlJournal = new SqlServerJournal(journalConfig);

            ExtractShardId = obj =>
            {
                switch (obj)
                {
                    case IShardId i:
                        return $"{i.ShardId}";
                    case CommandEnvelope commandEnvelope:
                        return commandEnvelope.EntityId.ToShardId(MaxNumberOfShards);
                    default:
                        throw new InvalidOperationException("Cannot derive shard identifier from unknown type");
                }
            };
            ExtractEntityId = obj =>
            {
                switch (obj)
                {
                    case CommandEnvelope commandEnvelope:
                        return ($"{commandEnvelope.EntityId}", commandEnvelope.Payload).ToTuple();
                    default:
                        throw new InvalidOperationException("Cannot derive entity identifier from unknown type");
                }
            };
        }

        /// <summary>
        ///     Plugin id for the query model
        /// </summary>
        /// <returns></returns>
        protected virtual Option<string> QueryPluginId { get; } = new Option<string>();

        /// <summary>
        ///     Registry name used for prefixing the shard region name
        /// </summary>
        /// <value></value>
        private Option<string> Name { get; } = Option<string>.None;

        /// <summary>
        ///     Underlying actor system
        /// </summary>
        /// <value></value>
        private ActorSystem ActorSystem { get; }

        /// <summary>
        ///     Sharding regions
        /// </summary>
        /// <value></value>
        private ClusterSharding Sharding { get; }

        /// <summary>
        ///     Global sharding settings
        /// </summary>
        /// <value></value>
        private ClusterShardingSettings ShardingSettings { get; }

        /// <summary>
        ///     Delegate for extracting entity id from incoming events
        /// </summary>
        /// <value></value>
        private ExtractEntityId ExtractEntityId { get; }

        /// <summary>
        ///     Delegate for extracting shard id from incoming events
        /// </summary>
        /// <value></value>
        private ExtractShardId ExtractShardId { get; }

        /// <summary>
        ///     Reference for extracting events by tag
        /// </summary>
        /// <value></value>
        private Option<IEventsByTagQuery> EventsByTagQuery { get; }

        /// <summary>
        ///     Registry of event types to entity names
        /// </summary>
        /// <returns></returns>
        private ConcurrentDictionary<Type, string> ReverseRegister { get; } = new ConcurrentDictionary<Type, string>();

        /// <summary>
        ///     Registry of entity names to entity types
        /// </summary>
        /// <returns></returns>
        private ConcurrentDictionary<string, Type> RegisteredTypeNames { get; } =
            new ConcurrentDictionary<string, Type>();

        /// <summary>
        ///     Timeout on entity reference Ask
        /// </summary>
        /// <value></value>
        private TimeSpan AskTimeout { get; }

        /// <summary>
        ///     Max number of available shards, used in hash based distribution
        /// </summary>
        /// <value></value>
        private int MaxNumberOfShards { get; }

        /// <summary>
        ///     Role for the current actor system
        /// </summary>
        /// <value></value>
        private Option<string> Role { get; }

        /// <summary>
        ///     Number of events to process before commiting a snapshot
        /// </summary>
        /// <value></value>
        private int SnapshotAfter { get; }

        /// <summary>
        ///     Reference to actor system termination
        /// </summary>
        public Task WhenTerminated => ActorSystem.WhenTerminated;

        /// <summary>
        ///     Get a reference to the entity with the given entityId
        /// </summary>
        /// <param name="entityId"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IShardedEntityReference RefFor<T>(string entityId)
            where T : class
        {
            if (!ReverseRegister.TryGetValue(typeof(T), out var entityName))
                throw new InvalidOperationException($"{typeof(T)} not registered.");

            return new ShardedEntityReference(
                entityId,
                Sharding.ShardRegion(PrependRegistryName(entityName)),
                ActorSystem,
                AskTimeout
            );
        }

        /// <summary>
        ///     Request to terminate the actor system
        /// </summary>
        /// <returns></returns>
        public Task Terminate()
        {
            return ActorSystem.Terminate();
        }


        /*

        def dslOffsetToOffset(dslOffset: LagomJavaDslOffset): Offset = dslOffset match {
            case uuid: LagomJavaDslOffset.TimeBasedUUID => TimeBasedUUID(uuid.value())
            case seq: LagomJavaDslOffset.Sequence       => Sequence(seq.value())
            case LagomJavaDslOffset.NONE                => NoOffset
            case _                                      => throw new IllegalArgumentException("Unsuppoerted offset type " + dslOffset.getClass.getName)
        }

        */

        public Source<(E, Offset), NotUsed> EventStream<E>(AggregateEventTag aggregateTag, Offset fromOffset)
            where E : AggregateEvent<E>
        {
            //var readJournal = PersistenceQuery.Get(ActorSystem).ReadJournalFor<SqlReadJournal>($"akka.persistence.query");

            if (!EventsByTagQuery.HasValue)
                throw new InvalidOperationException("No support for streaming events by tag");

            var queries = EventsByTagQuery.Value;
            var tag = aggregateTag.Tag;

            // TODO: Check this
            Offset MapStartingOffset(Offset o) => o;

            // TODO: Offset adapter stuff.
            //OffsetAdapter.dslOffsetToOffset(storedOffset)

            var startingOffset = MapStartingOffset(fromOffset);

            return queries.EventsByTag(tag, startingOffset)
                .Select(x =>
                {
                    ActorSystem.Log.Info($"{x.Event}, {x.Offset}");
                    return (x.Event as E, x.Offset);
                });

        }


        public void Register<T, TC, TE, TS>()
            where T : ShardedEntity<TC, TE, TS>, new()
            where TC : AbstractCommand
            where TE : AbstractEvent
            where TS : AbstractState
        {
            var prototype = new T();
            var entityTypeName = prototype.EntityTypeName;
            var entityClassType = prototype.GetType();

            var alreadyRegistered = RegisteredTypeNames.GetOrAdd(entityTypeName, entityClassType);
            if (alreadyRegistered != null && alreadyRegistered != entityClassType)
                throw new InvalidOperationException($"The entity type {nameof(T)} is already registered");

            ReverseRegister.TryAdd(entityClassType, entityTypeName);

            ActorSystem.Log.Warning("Passivate not set");

            var PassivateAfterIdleTimeout = TimeSpan.FromSeconds(100);

            // TODO: Plugin Ids
            const string snapshotPluginId = "";
            const string journalPluginId = "";

            JoinCluster(ActorSystem, "dev");

            if (Role.ForAll(Cluster.Get(ActorSystem).SelfRoles.Contains))
            {
                ActorSystem.Log.Info("Cluster sharding initialized");
                var props = ShardedEntityActorProps.Create<T, TC, TE, TS>(
                    entityTypeName,
                    Option<string>.None,
                    () => new T(),
                    SnapshotAfter,
                    PassivateAfterIdleTimeout,
                    snapshotPluginId,
                    journalPluginId
                );

                Sharding.Start(
                    entityTypeName,
                    props,
                    ShardingSettings,
                    ExtractEntityId,
                    ExtractShardId
                );
            }
            else
            {
                ActorSystem.Log.Warning("Cluster proxy initialized");
                Sharding.StartProxy(
                    entityTypeName,
                    Role.Value,
                    ExtractEntityId,
                    ExtractShardId
                );
            }
        }

        /// <summary>
        ///     Prepends the registry name ot the given entity type name
        /// </summary>
        /// <param name="entityTypeName">Entity type name</param>
        /// <returns></returns>
        /// <remarks>
        ///     The output is intended to be used as a unique name for
        ///     identifying the shard region
        /// </remarks>
        private string PrependRegistryName(string entityTypeName)
        {
            return (Name.HasValue ? Name.Value + "-" : "") + entityTypeName;
        }

        /// <summary>
        ///     Provider for self-joining on cluster registration
        /// </summary>
        /// <param name="actorSystem"></param>
        /// <param name="environment"></param>
        private void JoinCluster(ActorSystem actorSystem, string environment)
        {
            var config = actorSystem.Settings.Config.GetConfig("wyvern.cluster");

            var joinSelf = config.GetBoolean("join-self");
            var exitWhenDowned = config.GetBoolean("exit-when-downed");
            var isProd = string.Equals("prod", environment);

            if (isProd && joinSelf)
                actorSystem.Log.Warning("join-self should not be enabled in prod");

            var cluster = Cluster.Get(actorSystem);
            if (cluster.Settings.SeedNodes.IsEmpty && joinSelf)
                cluster.Join(cluster.SelfAddress);


            CoordinatedShutdown.Get(actorSystem)
                .AddTask(
                    CoordinatedShutdown.PhaseClusterShutdown,
                    "exit-when-downed",
                    async () =>
                    {
                        var reason = CoordinatedShutdown.Get(ActorSystem)
                                         .ShutdownReason == CoordinatedShutdown.ClusterDowningReason.Instance;
                        if (exitWhenDowned && reason)
                        {
                            var t = Task.Factory.StartNew(async () =>
                            {
                                Environment.Exit(-1);
                                await Task.CompletedTask;
                            });
                            await t.ConfigureAwait(true);
                            t.Start();
                        }

                        return await Task.FromResult(Done.Instance);
                    }
                );
        }
    }
}
