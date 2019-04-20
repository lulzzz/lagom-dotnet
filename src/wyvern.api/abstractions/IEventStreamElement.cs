using Akka.Persistence.Query;

namespace wyvern.api.abstractions
{
    public interface IEventStreamElement<TE>
        where TE : class
    {
        string EntityId { get; }
        TE Event { get; }
        Offset Offset { get; }
    }
}
