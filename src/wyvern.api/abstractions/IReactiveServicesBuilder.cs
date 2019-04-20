using System;
using Akka.Actor;
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
        IReactiveServicesBuilder AddReactiveService<T, TI>()
            where TI : T
            where T : Service;
        IReactiveServicesBuilder AddActorSystemDelegate(Action<ActorSystem> actorSystemDelegate);
        IReactiveServicesBuilder WithTopicSerializer<T>(Func<ISerializer> serializerFactory = null)
            where T : ISerializer, new();
        IReactiveServicesBuilder WithMessagePropertyExtractor<T>(Func<IMessagePropertyExtractor> extractorFactory = null)
            where T : IMessagePropertyExtractor, new();
    }
}