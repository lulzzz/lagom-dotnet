using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Dispatch.SysMsg;
using Akka.Persistence;
using Akka.Persistence.Journal;
using Akka.Streams.Util;
using wyvern.api.abstractions;
using wyvern.api.@internal.operations;
using wyvern.api.ioc;
using wyvern.entity.command;
using wyvern.entity.@event;
using wyvern.entity.@event.aggregate;
using wyvern.entity.state;
using wyvern.utils;

namespace wyvern.api.@internal.sharding
{
    /// <summary>
    /// Entity actor for sharded operations
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TC"></typeparam>
    /// <typeparam name="TE"></typeparam>
    /// <typeparam name="TS"></typeparam>
    internal sealed class ShardedEntityActor<T, TC, TE, TS> : PersistentActor
        where T : ShardedEntity<TC, TE, TS>, new()
        where TC : AbstractCommand
        where TE : AbstractEvent
        where TS : AbstractState
    {
        /// <summary>
        /// Separator used in delimiting the components of the persistent entity id
        /// </summary>
        private const char Separator = '|';

        /// <summary>
        /// Number of events recorded on this entity
        /// </summary>
        private long EventCount;

        private Func<SqlConnection> SqlConnectionFactory { get; }

        /// <summary>
        /// Public constructor (to be used only via Props)
        /// </summary>
        /// <param name="idPrefix"></param>
        /// <param name="entityId"></param>
        /// <param name="snapshotAfter"></param>
        /// <param name="passivateAfterIdleTimeout"></param>
        /// <param name="snapshotPluginId"></param>
        /// <param name="journalPluginId"></param>
        public ShardedEntityActor(
            string idPrefix,
            Option<string> entityId,
            int snapshotAfter,
            TimeSpan passivateAfterIdleTimeout,
            string snapshotPluginId,
            string journalPluginId
        )
        {
            PersistenceIdPrefix = idPrefix;
            EntityId = entityId.OrElse(
                Self.Path.Name
            );

            if (EntityId.IndexOf(Separator) > -1)
                throw new InvalidOperationException("Illegal use of separator character in entity name");

            Entity = new T
            {
                EntityId = EntityId
            };
            SnapshotAfter = snapshotAfter;
            PassivateAfterIdleTimeout = passivateAfterIdleTimeout;
            SnapshotPluginId = snapshotPluginId;
            JournalPluginId = journalPluginId;

            Context.SetReceiveTimeout(passivateAfterIdleTimeout);

            var constr = Context.System.Settings.Config.GetString("wyvern.persistence.ingestion.connection-string");
            SqlConnectionFactory = () => new SqlConnection(constr);

        }

        /**
         *   Entity Components
         */

        /// <summary>
        /// The entity object
        /// </summary>
        /// <value></value>
        private T Entity { get; }

        /// <summary>
        /// Persistence id prefix. Generally, the name of the actor system
        /// </summary>
        /// <value></value>
        private string PersistenceIdPrefix { get; }

        /// <summary>
        /// The entity Id
        /// </summary>
        /// <value></value>
        private string EntityId { get; }

        /// <summary>
        /// Persistence identifier for the given entity
        /// </summary>
        /// <value></value>
        public override string PersistenceId => $"{PersistenceIdPrefix}{Separator}{EntityId}";

        /**
         *   Lifetime Configuration
         */

        /// <summary>
        /// Number of events to occur before snapshot
        /// </summary>
        /// <value></value>
        private int SnapshotAfter { get; }

        /// <summary>
        /// Passivate this actor if no activity is noted within the given time span
        /// </summary>
        /// <value></value>
        private TimeSpan PassivateAfterIdleTimeout { get; }

        /**
         *   Behavior Configuration
         */

        /// <summary>
        /// Event handlers delegated to the entity behavior
        /// </summary>
        private IReadOnlyDictionary<Type, Func<TE, ShardedEntity<TC, TE, TS>.Behavior, ShardedEntity<TC, TE, TS>.Behavior>>
            EventHandlers => Entity.BehaviorProperty.EventHandlers;

        /// <summary>
        /// Command handlers delegated to the entity behavior
        /// </summary>
        private IReadOnlyDictionary<Type, Func<TC, ShardedEntity<TC, TE, TS>.ICommandContext<TC>, IPersist<TE>>>
            CommandHandlers => Entity.BehaviorProperty.CommandHandlers;

        private bool HandleCommand(Type commandType, object message)
        {
            if (!CommandHandlers.TryGetValue(commandType, out var commandHandler))
            {
                Log.Warning($"Unknown command type received: {commandType}");
                return false;
            }

            // TODO: possible to have ambiguous match, ignore this for a bit..
            //var replyType = commandType.GetInterface(typeof(IReplyType<>).Name).GetGenericArguments()[0];

            var commandContext = newContext(Sender, SqlConnectionFactory);

            try
            {
                var command = (TC)message;
                var result = commandHandler.Invoke(command, commandContext);
                var resultType = result.GetType();
                var resultGenericTypeDef = resultType.GetGenericTypeDefinition();

                if (resultGenericTypeDef == typeof(PersistNone<>)) return true;

                if (resultGenericTypeDef == typeof(PersistOne<>))
                {
                    var e = (TE)resultType.GetProperty("Event").GetValue(result, null);
                    var ap = resultType.GetProperty("AfterPersist").GetValue(result, null);

                    ApplyEvent(e);
                    Persist(Tag(e), x =>
                    {
                        try
                        {
                            EventCount++;
                            if (ap != null)
                            {
                                var mi = ap.GetType().GetMethod("Invoke");
                                mi.Invoke(ap, new object[] { e });
                            }
                            if (SnapshotAfter > 0 && EventCount % SnapshotAfter == 0)
                                SaveSnapshot(Entity.BehaviorProperty.State);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Failed to execute persist side-effect on command");
                            commandContext.CommandFailed(ex);
                            throw ex;
                        }
                    });
                    return true;
                }

                if (resultGenericTypeDef == typeof(PersistAll<>))
                {
                    var events = (ImmutableArray<TE>)resultType
                        .GetProperty(
                            "Events",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                        )
                        .GetValue(result, null);

                    var ap = resultType.GetProperty(
                            "AfterPersist",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                        ).GetValue(result, null);

                    var count = events.Length;
                    var snap = false;

                    foreach (var @event in events)
                        ApplyEvent(@event);

                    PersistAll(
                        events.Select(x => Tag(x)),
                        evt =>
                        {
                            try
                            {
                                EventCount += 1;
                                count -= 1;

                                if (ap != null && count == 0)
                                {
                                    var mi = ap.GetType().GetMethod("Invoke");
                                    mi.Invoke(ap, null);
                                }

                                if (SnapshotAfter > 0 && EventCount % SnapshotAfter == 0)
                                    snap = true;
                                if (count == 0 && snap)
                                    SaveSnapshot(Entity.BehaviorProperty.State);
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Failed to execute persistAll side-effect on command");
                                commandContext.CommandFailed(ex);
                                throw ex;
                            }
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                // Non fatal.
                Log.Error(ex, "Failed to evaluate command");
                commandContext.CommandFailed(ex);
                throw;
            }

            return true;
        }

        /// <summary>
        /// Entrypoint for receiving a command
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        protected override bool ReceiveCommand(object message)
        {
            var commandType = message.GetType();

            // Normal command handler
            if (message is TC)
            {
                var handled = HandleCommand(commandType, message);
                if (!handled)
                {
                    var commandContext = newContext(Sender, SqlConnectionFactory);
                    commandContext.InvalidCommand("Invalid command");
                }
            }
            // Successful snapshot retention
            else if (commandType == typeof(SaveSnapshotSuccess))
            {
                return true;
            }
            // Actor due for passivation
            else if (message is ReceiveTimeout)
            {
                ShardRegion.Context.Parent.Tell(
                    new Passivate(new Stop())
                );
            }
            // Cluster prescribing passivation
            else if (message is Stop || message is PoisonPill)
            {
                Context.Stop(Self);
            }
            // Unhandled type
            else
            {
                Log.Warning($"Invalid command type received: illegal base class {commandType}");
            }

            return true;
        }

        // TODO: check this
        bool _init = false;

        /// <summary>
        /// Recover or initialize the entity state
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        protected override bool ReceiveRecover(object message)
        {
            void InitEmpty()
            {
                if (_init) return;
                var initial = Entity.InitialBehavior(Option<TS>.None);
                Entity.BehaviorProperty = initial;
                _init = true;
            };

            switch (message)
            {
                case SnapshotOffer s:
                    if (!_init)
                    {
                        var initial = Entity.InitialBehavior(new Option<TS>((TS)s.Snapshot));
                        Entity.BehaviorProperty = initial;
                        _init = true;
                    }
                    break;

                case RecoveryCompleted c:
                    InitEmpty();
                    var newBehavior = Entity.RecoveryCompleted();
                    Entity.BehaviorProperty = newBehavior;
                    break;

                case TE e:
                    InitEmpty();
                    ApplyEvent(e);
                    EventCount += 1;
                    break;
            }

            return true;
        }

        /// <summary>
        /// Apply given event to the entity, thereby replacing its behavior set
        /// </summary>
        /// <param name="e"></param>
        private void ApplyEvent(TE e)
        {
            if (EventHandlers.TryGetValue(e.GetType(), out var eventHandler))
            {
                var newBehavior = eventHandler.Invoke(e, Entity.BehaviorProperty);
                Entity.BehaviorProperty = newBehavior;
                return;
            }

            Log.Warning($"Unhandled event type {typeof(TE)}");
        }

        /// <summary>
        /// Closure over sender
        /// </summary>
        /// <typeparam name="TC"></typeparam>
        /// <returns></returns>
        private ShardedEntity<TC, TE, TS>.CommandContext<TC> newContext(IActorRef sender, Func<SqlConnection> sqlConnectionFactory)
        {
            return new ShardedEntity<TC, TE, TS>.IngestionCommandContext<TC>(sender, sqlConnectionFactory);
        }

        /// <summary>
        /// Obtain tag for given event
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private object Tag(TE e)
        {
            switch (e)
            {
                case IAggregateEvent ev:
                    IAggregateEventTag tag;
                    switch (ev.AggregateTag)
                    {
                        case IAggregateEventTag t:
                            tag = t;
                            break;
                        case IAggregateEventShards s:
                            var mi = s.GetType().GetMethod("ForEntityId");
                            tag = (IAggregateEventTag)mi.Invoke(s, new object[] { EntityId });
                            break;
                        default:
                            throw new InvalidOperationException("Unknown aggregate event type");
                    }

                    return new Tagged(ev, new[] { tag.Tag });
                default:
                    return e;
            }
        }
    }
}
