using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data.SqlClient;
using Akka.Actor;
using Akka.Streams.Util;
using wyvern.api.abstractions;
using wyvern.api.@internal.behavior;
using wyvern.api.@internal.command;
using wyvern.api.@internal.operations;
using wyvern.entity.command;
using wyvern.entity.@event;
using wyvern.entity.state;

namespace wyvern.api.ioc
{
    /// <summary>
    /// Represents a sharded entity containing a the entity's behavior and
    /// given state
    /// </summary>
    /// <typeparam name="TC"></typeparam>
    /// <typeparam name="TE"></typeparam>
    /// <typeparam name="TS"></typeparam>
    public abstract class ShardedEntity<TC, TE, TS>
        where TC : AbstractCommand
        where TE : AbstractEvent
        where TS : AbstractState
    {
        /// <summary>
        /// Current Behavior instance
        /// </summary>
        /// <value></value>
        public Behavior BehaviorProperty { get; internal set; }

        /// <summary>
        /// Entity unique identifier
        /// </summary>
        /// <value></value>
        public string EntityId { get; internal set; }

        /// <summary>
        /// Type name of current entity
        /// </summary>
        /// <returns></returns>
        public string EntityTypeName => GetType().Name;

        /// <summary>
        /// Reference to state of current behaviour
        /// </summary>
        protected TS State => BehaviorProperty.State;

        /// <summary>
        /// Abstract prompt for setting the initial behavior.  To be implemented
        /// by extending class
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public abstract Behavior InitialBehavior(Option<TS> state);

        /// <summary>
        /// Create a new typed instance of BehaviorBuilder
        /// </summary>
        /// <param name="state"></param>
        /// <typeparam name="TC"></typeparam>
        /// <typeparam name="TE"></typeparam>
        /// <typeparam name="TS"></typeparam>
        /// <returns></returns>
        protected IBehaviorBuilder<TC, TE, TS> NewBehaviorBuilder(TS state)
        {
            return new BehaviorBuilder<TC, TE, TS>(state);
        }

        /// <summary>
        /// Event to call when recovery is completed
        /// </summary>
        /// <returns></returns>
        public Behavior RecoveryCompleted()
        {
            return BehaviorProperty;
        }

        /// <summary>
        /// Behavior (convenience class)
        /// </summary>
        public sealed class Behavior
        {
            /// <summary>
            /// Behavior constructor
            /// </summary>
            /// <param name="commandHandlers"></param>
            /// <param name="eventHandlers"></param>
            /// <param name="state"></param>
            public Behavior(
                IReadOnlyDictionary<Type, Func<TC, ICommandContext<TC>, IPersist<TE>>> commandHandlers,
                IReadOnlyDictionary<Type, Func<TE, Behavior, Behavior>> eventHandlers,
                TS state)
            {
                CommandHandlers = commandHandlers;
                EventHandlers = eventHandlers;
                State = state;
            }

            /// <summary>
            /// Current state of the given entity
            /// </summary>
            /// <value></value>
            internal TS State { get; }

            /// <summary>
            /// Map of command types to command handlers (readonly, immutable)
            /// </summary>
            /// <value></value>
            public IReadOnlyDictionary<Type, Func<TC, ICommandContext<TC>, IPersist<TE>>> CommandHandlers { get; }

            /// <summary>
            /// Map of event types to event handlers (readonly, immutable)
            /// </summary>
            /// <value></value>
            public IReadOnlyDictionary<Type, Func<TE, Behavior, Behavior>> EventHandlers { get; }

            /// <summary>
            /// Create a new Behavior with the given state.  Only accessible
            /// internally within scope of the invocation on a given event handler.
            /// </summary>
            /// <param name="newState">State to apply to the behavior</param>
            /// <typeparam name="TS2">State type</typeparam>
            /// <returns></returns>
            internal Behavior WithState<TS2>(TS2 newState)
                where TS2 : TS
            {
                return new Behavior(CommandHandlers, EventHandlers, newState);
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Command context interface
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public interface ICommandContext<T> : IReadOnlyCommandContext
        {
            /// <summary>
            /// Persist and after persist handlers
            /// </summary>
            /// <param name="e"></param>
            /// <param name="afterPersist"></param>
            /// <returns></returns>
            IPersist<TE> ThenPersist(TE e, Action<TE> afterPersist = null);

            /// <summary>
            /// Persist all and after persist handlers
            /// </summary>
            /// <param name="e"></param>
            /// <param name="afterPersist"></param>
            /// <returns></returns>
            IPersist<TE> ThenPersistAll(ImmutableArray<TE> e, Action afterPersist = null);

            /// <summary>
            /// Done handler
            /// </summary>
            /// <returns></returns>
            IPersist<TE> Done();
        }

        public interface IIngestionCommandContext<T> : ICommandContext<T>
        {
            SqlConnection SqlConnection();
        }

        internal class IngestionCommandContext<T> : CommandContext<T>, IIngestionCommandContext<T>
            where T : class
        {
            internal Func<SqlConnection> DbConnectionFactory { get; }

            public IngestionCommandContext(IActorRef sender, Func<SqlConnection> dbConnectionFactory)
                : base(sender)
            {
                DbConnectionFactory = dbConnectionFactory;
            }

            public SqlConnection SqlConnection()
            {
                return DbConnectionFactory.Invoke();
            }
        }

        /// <summary>
        /// Command context
        /// </summary>
        /// <typeparam name="T"></typeparam>
        internal class CommandContext<T> : ReadOnlyCommandContext, ICommandContext<T>
            where T : class
        {

            /// <summary>
            /// CommandContext constructor
            /// </summary>
            /// <param name="sender">Sending actor</param>
            /// <returns></returns>
            public CommandContext(IActorRef sender) : base(sender)
            {
            }

            /// <inheritdoc />
            /// <summary>
            /// Within scope of command execution, persist the given event.
            /// Subsequently, may execute the 'afterPersist' action for any
            /// side effects.
            /// </summary>
            /// <param name="e"></param>
            /// <param name="afterPersist"></param>
            /// <typeparam name="E"></typeparam>
            /// <returns></returns>
            public IPersist<TE> ThenPersist(TE e, Action<TE> afterPersist = null)
            {
                return new PersistOne<TE>(
                    e,
                    afterPersist ?? (_ => Reply(Akka.Done.Instance))
                );
            }

            /// <summary>
            /// Persist the events atomically and execute the side-effect once for each
            /// event.
            /// </summary>
            /// <param name="e"></param>
            /// <param name="afterPersist"></param>
            /// <returns></returns>
            public IPersist<TE> ThenPersistAll(ImmutableArray<TE> e, Action afterPersist = null)
            {
                return new PersistAll<TE>(
                    e,
                    afterPersist ?? (() => Reply(Akka.Done.Instance))
                );
            }

            /// <inheritdoc />
            /// <summary>
            /// Indicate that command execution is done
            /// </summary>
            /// <typeparam name="E"></typeparam>
            /// <returns></returns>
            public IPersist<TE> Done()
            {
                return new PersistNone<TE>();
            }

            /// <summary>
            ///
            /// </summary>
            /// <param name="e"></param>
            /// <param name="afterPersist"></param>
            /// <typeparam name="TE2"></typeparam>
            /// <returns></returns>
            /// <exception cref="NotImplementedException"></exception>
            public IPersist<TE2> ThenPersist<TE2>(TE2 e, Action<TE2> afterPersist) where TE2 : AbstractEvent
            {
                throw new NotImplementedException();
            }

            /// <summary>
            ///
            /// </summary>
            /// <typeparam name="TE2"></typeparam>
            /// <returns></returns>
            /// <exception cref="NotImplementedException"></exception>
            public IPersist<TE2> Done<TE2>() where TE2 : AbstractEvent
            {
                throw new NotImplementedException();
            }
        }
    }
}
