using wyvern.entity.@event;

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
