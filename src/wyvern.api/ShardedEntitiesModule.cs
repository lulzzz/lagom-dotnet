using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using wyvern.api.@internal.surfaces;
using wyvern.api.ioc;

namespace wyvern.api
{
    public static class ShardedEntitiesModule
    {
        public static IServiceCollection AddShardedEntities(this IServiceCollection services,
            Action<IShardedEntityRegistryBuilder> builderDelegate)
        {
            services.AddSingleton<IShardedEntityRegistry>(x =>
            {
                var builder = new ShardedEntityRegistryBuilder();
                builderDelegate.Invoke(builder);
                // TODO: find a way to prepare akka before this.
                return builder.Build();
            });
            return services;
        }
    }
}
