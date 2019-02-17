using wyvern.api.abstractions;
using wyvern.entity.@event;
using wyvern.utils;

namespace wyvern.api.@internal.operations
{
    /// <summary>
    /// Read-only event marker
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    [Immutable]
    internal sealed class PersistNone<TEvent> : IPersist<TEvent>
        where TEvent : AbstractEvent
    {
    }
}
