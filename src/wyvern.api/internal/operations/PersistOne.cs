using System;
using wyvern.entity.@event;

namespace wyvern.api.@internal.operations
{
    /// <summary>
    ///     Event marker for persistence
    /// </summary>
    /// <typeparam name="E"></typeparam>
    [Immutable]
    internal sealed class PersistOne<E> : IPersist<E>
        where E : AbstractEvent
    {
        /// <summary>
        ///     Event
        /// </summary>
        /// <value></value>
        public E Event { get; }

        /// <summary>
        ///     Delegate event wrapper for post-action side effects
        /// </summary>
        /// <value></value>
        public Action<E> AfterPersist { get; }

        /// <summary>
        ///     Constructor
        /// </summary>
        public PersistOne(E @event, Action<E> afterPersist) =>
            (Event, AfterPersist) = (@event, afterPersist);

    }
}
