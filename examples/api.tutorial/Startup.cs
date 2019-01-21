using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using Akka.Visualize;
using wyvern.api;
using wyvern.api.ioc;
using wyvern.entity;
using wyvern.entity.command;
using wyvern.entity.@event;
using wyvern.entity.state;

namespace api.tutorial
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // TODO: Automatic registration of these types would be helpful
            services.AddReactiveServices(x =>
            {
                x.AddReactiveService<BankAccountService, BankAccountServiceImpl>();
                x.AddActorSystemDelegate(system =>
                {
                    var wv = new WebApiVisualizer();    
                    ActorVisualizeExtension.InstallVisualizer(system, wv);
                });
            });
            services.AddShardedEntities(x =>
            {
                x.WithShardedEntity<BankAccountEntity, BankAccountCommand, BankAccountEvent, BankAccountState>();
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseMiddleware<HttpStatusCodeExceptionMiddleware>();
            app.UseReactiveServices();
        }
    }
}
