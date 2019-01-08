using System;
using System.Collections.Immutable;
using wyvern.entity.@event;

namespace wyvern.api.@internal.operations
{
    /// <summary>
    ///     Aggregate event marker for persistance
    /// </summary>
    /// <typeparam name="E"></typeparam>
    [Immutable]
    internal sealed class PersistAll<E>
        where E : IEvent
    {
        /// <summary>
        ///     Events array
        /// </summary>
        /// <value></value>
        internal ImmutableArray<E> Events { get; }

        /// <summary>
        ///     After persist effect
        /// </summary>
        /// <value></value>
        internal Action<E> AfterPersist { get; }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="events"></param>
        /// <param name="afterPersist"></param>
        /// <returns></returns>
        public PersistAll(ImmutableArray<E> events, Action<E> afterPersist) =>
            (Events, AfterPersist) = (events, afterPersist);

    }
}
