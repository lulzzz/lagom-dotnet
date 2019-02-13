using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using wyvern.api.ioc;
using wyvern.entity.command;
using wyvern.entity.@event;
using wyvern.entity.state;

namespace wyvern.api.@internal.behavior
{
    /// <inheritdoc />
    /// <summary>
    ///     Behavior builder which is typed to a given entity.
    ///     Assists with generating an immutable Behavior object.  Note, the
    ///     builder itself is not immutable as the object is rather transient.
    /// </summary>
    /// <typeparam name="TC"></typeparam>
    /// <typeparam name="TE"></typeparam>
    /// <typeparam name="TS"></typeparam>
    [Immutable]
    internal abstract class BehaviorBuilderImpl<TC, TE, TS> : IBehaviorBuilder<TC, TE, TS>
        where TC : AbstractCommand
        where TE : AbstractEvent
        where TS : AbstractState
    {
        /// <summary>
        /// Behavior builder constructor
        /// </summary>
        /// <param name="state"></param>
        /// <param name="eventHandlers"></param>
        /// <param name="commandHandlers"></param>
        protected BehaviorBuilderImpl(
            TS state,
            IReadOnlyDictionary<Type, Func<TE, ShardedEntity<TC, TE, TS>.Behavior, ShardedEntity<TC, TE, TS>.Behavior>> eventHandlers,
            IReadOnlyDictionary<Type, Func<TC, ShardedEntity<TC, TE, TS>.ICommandContext<TC>, IPersist<TE>>> commandHandlers)
        {
            State = state;
            EventHandlers = ImmutableDictionary
                .Create<Type, Func<TE, ShardedEntity<TC, TE, TS>.Behavior, ShardedEntity<TC, TE, TS>.Behavior>>()
                .AddRange(eventHandlers);
            CommandHandlers = ImmutableDictionary
                .Create<Type, Func<TC, ShardedEntity<TC, TE, TS>.ICommandContext<TC>, IPersist<TE>>>()
                .AddRange(commandHandlers);
        }

        /// <summary>
        /// DbConnectionFactory
        /// </summary>
        /// <value></value>
        protected Func<SqlConnection> IngestionConnectionFactory { get; }

        /// <summary>
        /// Entity state
        /// </summary>
        /// <value></value>
        private TS State { get; }

        /// <summary>
        ///     Map of command types to command handlers
        /// </summary>
        /// <value></value>
        private ImmutableDictionary<Type, Func<TC, ShardedEntity<TC, TE, TS>.ICommandContext<TC>, IPersist<TE>>> CommandHandlers { get; }

        /// <summary>
        ///     Map of event types to event handlers
        /// </summary>
        /// <value></value>
        private ImmutableDictionary<Type, Func<TE, ShardedEntity<TC, TE, TS>.Behavior, ShardedEntity<TC, TE, TS>.Behavior>> EventHandlers
        { get; }

        /// <summary>
        ///     Set the command handler for the given type
        /// </summary>
        /// <param name="func"></param>
        /// <typeparam name="TC2"></typeparam>
        /// <typeparam name="TR"></typeparam>
        /// <returns></returns>
        public IBehaviorBuilder<TC, TE, TS> SetCommandHandler<TC2, TR>(
            Func<TC2, ShardedEntity<TC, TE, TS>.ICommandContext<TC>, IPersist<TE>> func)
            where TC2 : IReplyType<TR>, TC
            where TR : class
            => new BehaviorBuilder<TC, TE, TS>(
                State,
                EventHandlers,
                CommandHandlers.Add(
                    typeof(TC2),
                    (ctx, e) =>
                    {
                        var d = func.Invoke((TC2)ctx, e);
                        return d;
                    }
                )
            );

        public IBehaviorBuilder<TC, TE, TS> SetIngestionCommandHandler<TC2, TR2>(
            Func<TC2, ShardedEntity<TC, TE, TS>.IIngestionCommandContext<TC>, IPersist<TE>> func)
            where TC2 : IReplyType<TR2>, TC
            where TR2 : class
            => new BehaviorBuilder<TC, TE, TS>(
                State,
                EventHandlers,
                CommandHandlers.Add(
                    typeof(TC2),
                    (ctx, e) =>
                    {
                        var d = func.Invoke((TC2)ctx, e as ShardedEntity<TC, TE, TS>.IIngestionCommandContext<TC>);
                        return d;
                    }
                )
            );

        /// <summary>
        ///     Set the event handler for the given type
        /// </summary>
        /// <param name="func"></param>
        /// <typeparam name="TE2"></typeparam>
        /// <typeparam name="TS2"></typeparam>
        /// <returns></returns>
        public IBehaviorBuilder<TC, TE, TS> SetEventHandler<TE2, TS2>(
            Func<TE2, TS2> func)
            where TE2 : TE
            where TS2 : TS
            => new BehaviorBuilder<TC, TE, TS>(
                State,
                EventHandlers.Add(
                    typeof(TE2),
                    (e, b) => b.WithState(func.Invoke((TE2)e))
                ),
                CommandHandlers
            );

        /// <summary>
        ///     Set the event handler for the given type
        /// </summary>
        /// <param name="func"></param>
        /// <typeparam name="TE2"></typeparam>
        /// <typeparam name="TS2"></typeparam>
        /// <returns></returns>
        public IBehaviorBuilder<TC, TE, TS> SetEventHandlerChangingBehavior<TE2, TS2>
            (Func<TE2, ShardedEntity<TC, TE, TS>.Behavior> func)
            where TE2 : TE
            => new BehaviorBuilder<TC, TE, TS>(
                State,
                EventHandlers.Add(
                    typeof(TE2),
                    (e, b) => func.Invoke((TE2)e)
                ),
                CommandHandlers
            );

        /// <summary>
        ///     Set a readonly command handler for the given type
        ///     (Readonly command does not impact entity state)
        /// </summary>
        /// <param name="func"></param>
        /// <typeparam name="TC2"></typeparam>
        /// <typeparam name="TR2"></typeparam>
        /// <returns></returns>
        public IBehaviorBuilder<TC, TE, TS> SetReadOnlyCommandHandler<TC2, TR2>
            (Action<TC2, IReadOnlyCommandContext> func)
            where TC2 : TC, IReplyType<TR2>
            where TR2 : class
            => new BehaviorBuilder<TC, TE, TS>(
                State,
                EventHandlers,
                CommandHandlers.Add(
                    typeof(TC2),
                    (ctx, e) =>
                    {
                        func.Invoke((TC2)ctx, e);
                        return e.Done();
                    }
                )
            );

        /// <summary>
        ///     Build the current Behavior model
        /// </summary>
        /// <returns></returns>
        [NoSideEffects]
        public ShardedEntity<TC, TE, TS>.Behavior Build()
        {
            return new ShardedEntity<TC, TE, TS>.Behavior(CommandHandlers, EventHandlers, State);
        }
    }
}
