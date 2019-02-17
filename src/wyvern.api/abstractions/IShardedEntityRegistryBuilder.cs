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
        IShardedEntityRegistryBuilder WithExtension<TExtension, TImplementation>()
            where TExtension : class, IExtension
            where TImplementation : IExtensionId;
        
        IShardedEntityRegistryBuilder WithReadSide<TEvent, TProcessor>()
            where TEvent : AggregateEvent<TEvent>
            where TProcessor : ReadSideProcessor<TEvent>, new();
        
        IShardedEntityRegistryBuilder WithShardedEntity<T, TCommand, TEvent, TState>()
            where T : ShardedEntity<TCommand, TEvent, TState>, new()
            where TCommand : AbstractCommand
            where TEvent : AbstractEvent
            where TState : AbstractState;

        IShardedEntityRegistry Build();
    }
}
