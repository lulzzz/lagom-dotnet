using wyvern.api.abstractions;
using wyvern.entity.@event;
using wyvern.utils;

namespace wyvern.api.@internal.operations
{
    /// <summary>
    ///     Read-only event marker
    /// </summary>
    /// <typeparam name="E"></typeparam>
    [Immutable]
    internal sealed class PersistNone<E> : IPersist<E>
        where E : AbstractEvent
    {
    }
}
