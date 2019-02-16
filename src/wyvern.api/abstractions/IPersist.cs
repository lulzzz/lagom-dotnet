using wyvern.entity.@event;

namespace wyvern.api.abstractions
{
    /// <summary>
    ///     Persistence interface
    /// </summary>
    /// <typeparam name="TE"></typeparam>
    public interface IPersist<out TE>
        where TE : AbstractEvent
    {
    }
}
