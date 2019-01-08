using wyvern.entity.@event;

namespace wyvern.api
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
