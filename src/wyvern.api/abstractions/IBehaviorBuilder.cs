using System;
using System.Data.SqlClient;
using wyvern.api.ioc;
using wyvern.entity.command;
using wyvern.entity.@event;
using wyvern.entity.state;

namespace wyvern.api
{
    /// <summary>
    /// BehaviorBuilder interface
    /// </summary>
    /// <typeparam name="TC">Command type</typeparam>
    /// <typeparam name="TE">Event type</typeparam>
    /// <typeparam name="TS">State type</typeparam>
    [Immutable]
    public interface IBehaviorBuilder<TC, TE, TS>
        where TC : AbstractCommand
        where TE : AbstractEvent
        where TS : AbstractState
    {
        IBehaviorBuilder<TC, TE, TS> SetCommandHandler<TC2, TR2>(
            Func<TC2, ShardedEntity<TC, TE, TS>.ICommandContext<TC>, IPersist<TE>> func)
            where TC2 : IReplyType<TR2>, TC
            where TR2 : class;

        IBehaviorBuilder<TC, TE, TS> SetEventHandler<TE2, TS2>(Func<TE2, TS2> func)
            where TE2 : TE
            where TS2 : TS;

        IBehaviorBuilder<TC, TE, TS> SetEventHandlerChangingBehavior<TE2, TS2>(
            Func<TE2, ShardedEntity<TC, TE, TS>.Behavior> func)
            where TE2 : TE;

        IBehaviorBuilder<TC, TE, TS> SetIngestionCommandHandler<TC2, TR2>(
            Func<TC2, ShardedEntity<TC, TE, TS>.IIngestionCommandContext<TC>, IPersist<TE>> func)
            where TC2 : IReplyType<TR2>, TC
            where TR2 : class;

        IBehaviorBuilder<TC, TE, TS> SetReadOnlyCommandHandler<TC2, TR2>(
            Action<TC2, IReadOnlyCommandContext> func)
            where TC2 : TC, IReplyType<TR2>
            where TR2 : class;

        ShardedEntity<TC, TE, TS>.Behavior Build();
    }
}
