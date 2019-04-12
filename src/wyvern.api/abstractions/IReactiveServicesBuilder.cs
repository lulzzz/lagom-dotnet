using wyvern.api.ioc;
using wyvern.entity.command;
using wyvern.entity.@event;
using wyvern.entity.state;

namespace wyvern.api.abstractions
{
    public interface IReactiveServiceBuilder
    {
        IReactiveServiceBuilder AddShardedEntity<T, TC, TE, TS>()
        where T : ShardedEntity<TC, TE, TS>, new()
        where TC : AbstractCommand
        where TE : AbstractEvent
        where TS : AbstractState;
    }

    public interface IReactiveServicesBuilder
    {
        ReactiveServicesBuilder AddReactiveService<T, TI>()
        where TI : T
        where T : Service;
    }
}