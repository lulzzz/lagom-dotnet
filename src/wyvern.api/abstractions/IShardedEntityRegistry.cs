using System.Threading.Tasks;
using Akka;
using Akka.Persistence.Query;
using Akka.Streams.Dsl;
using wyvern.api.ioc;
using wyvern.entity.command;
using wyvern.entity.@event;
using wyvern.entity.@event.aggregate;
using wyvern.entity.state;

namespace wyvern.api.abstractions
{
    public interface IShardedEntityRegistry
    {
        // TODO: event type embedded within typed registry?
        Source<(TE, Offset), NotUsed> EventStream<TE>(AggregateEventTag instance, Offset fromOffset)
            where TE : AggregateEvent<TE>;

        IShardedEntityReference RefFor<T>(string entityId) where T : class;
        void Register<T, TC, TE, TS>()
            where T : ShardedEntity<TC, TE, TS>, new()
            where TC : AbstractCommand
            where TE : AbstractEvent
            where TS : AbstractState;

        Task Terminate();
    }
}
