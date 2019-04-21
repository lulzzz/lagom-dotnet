using System;
using Akka.Actor;
using Akka.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using wyvern.api.abstractions;
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
                // TODO: this can just be registered and invoked on the other end...
                var actorSystem = x.GetService<ActorSystem>();
                // TODO: Clean up config so it's directed.
                var builder = new ShardedEntityRegistryBuilder(actorSystem, x.GetService<IConfiguration>(), x.GetService<Config>());

                builderDelegate.Invoke(builder);

                return builder.Build();
            });
            return services;
        }
    }
}
