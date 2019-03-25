using System;
using System.Collections.Immutable;
using wyvern.api.abstractions;
using wyvern.entity.@event;
using wyvern.utils;

namespace wyvern.api.@internal.operations
{
    /// <summary>
    /// Aggregate event marker for persistance
    /// </summary>
    /// <typeparam name="E"></typeparam>
    [Immutable]
    internal sealed class PersistAll<E> : IPersist<E>
        where E : AbstractEvent
    {
        /// <summary>
        /// Events array
        /// </summary>
        /// <value></value>
        internal ImmutableArray<E> Events { get; }

        /// <summary>
        /// After persist effect
        /// </summary>
        /// <value></value>
        internal Action AfterPersist { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="events"></param>
        /// <param name="afterPersist"></param>
        /// <returns></returns>
        public PersistAll(ImmutableArray<E> events, Action afterPersist) =>
            (Events, AfterPersist) = (events, afterPersist);

    }
}
