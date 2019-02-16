using System;
using System.Collections.Generic;
using wyvern.api.ioc;
using wyvern.entity.command;
using wyvern.entity.@event;
using wyvern.entity.state;

namespace wyvern.api.@internal.behavior
{
    /// <summary>
    ///     Surface of the of the behavior builder which helps build an
    /// immutable behavior for an entity command, event, state mapping.
    /// </summary>
    /// <typeparam name="TC"></typeparam>
    /// <typeparam name="TE"></typeparam>
    /// <typeparam name="TS"></typeparam>
    [Immutable]
    internal class BehaviorBuilder<TC, TE, TS> : BehaviorBuilderImpl<TC, TE, TS>
        where TC : AbstractCommand
        where TE : AbstractEvent
        where TS : AbstractState
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="state"></param>
        /// <param name="eventHandlers"></param>
        /// <param name="commandHandlers"></param>
        internal BehaviorBuilder(
            TS state,
            IReadOnlyDictionary<Type, Func<TE, ShardedEntity<TC, TE, TS>.Behavior, ShardedEntity<TC, TE, TS>.Behavior>> eventHandlers,
            IReadOnlyDictionary<Type, Func<TC, ShardedEntity<TC, TE, TS>.ICommandContext<TC>, IPersist<TE>>> commandHandlers)
            : base(state, eventHandlers, commandHandlers)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="state"></param>
        internal BehaviorBuilder(
            TS state
        ) : base(
            state,
            new Dictionary<Type, Func<TE, ShardedEntity<TC, TE, TS>.Behavior, ShardedEntity<TC, TE, TS>.Behavior>>(),
            new Dictionary<Type, Func<TC, ShardedEntity<TC, TE, TS>.ICommandContext<TC>, IPersist<TE>>>()
        )
        {
        }

    }
}
