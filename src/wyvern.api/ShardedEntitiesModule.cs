using System;
using Akka.Actor;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using wyvern.api.@internal.surfaces;
using wyvern.api.ioc;

namespace wyvern.api
{
    public static class ShardedEntitiesModule
    {
        static bool WasCalled { get; set; }

        public static IServiceCollection AddShardedEntities(this IServiceCollection services,
            Action<IShardedEntityRegistryBuilder> builderDelegate)
        {
            if (WasCalled) throw new Exception("ShardedEntitiesModule already called");
            WasCalled = true;

            services.AddSingleton(x =>
            {
                var actorSystem = x.GetService<ActorSystem>();
                var builder = new ShardedEntityRegistryBuilder(actorSystem, x.GetService<IConfiguration>());
                
                builderDelegate.Invoke(builder);
                
                return builder.Build();
            });
            return services;
        }
    }
}
