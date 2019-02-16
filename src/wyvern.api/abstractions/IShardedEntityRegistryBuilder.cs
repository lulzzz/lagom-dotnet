using Akka.Actor;
using wyvern.api.@internal.readside;
using wyvern.api.ioc;
using wyvern.entity.command;
using wyvern.entity.@event;
using wyvern.entity.@event.aggregate;
using wyvern.entity.state;

namespace wyvern.api.abstractions
{
    public interface IShardedEntityRegistryBuilder
    {
        IShardedEntityRegistryBuilder WithExtension<T, TI>()
            where T : class, IExtension
            where TI : IExtensionId;
        
        IShardedEntityRegistryBuilder WithReadSide<TE, TP>()
            where TE : AggregateEvent<TE>
            where TP : ReadSideProcessor<TE>, new();
        
        IShardedEntityRegistryBuilder WithShardedEntity<T, C, E, S>()
            where T : ShardedEntity<C, E, S>, new()
            where C : AbstractCommand
            where E : AbstractEvent
            where S : AbstractState;

        IShardedEntityRegistry Build();
    }
}
