using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Akka.Visualize;
using wyvern.api;
using wyvern.api.ioc;
using static wyvern.api.ioc.ServiceExtensions;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddCors();

        services.AddShardedEntities(x =>
        {
            x.WithShardedEntity<HelloEntity, HelloCommand, HelloEvent, HelloState>();
            // x.WithReadSide<ArticleWebsiteDisplayRuleEvent, ArticleWebsiteDisplayRuleReadSideProcessor>();
        });

        services.AddReactiveServices(x =>
        {
            x.AddReactiveService<HelloService, HelloServiceImpl>();
            x.AddActorSystemDelegate(system =>
            {
                // TODO: disconnect between WithVisualizer option...
                //ActorVisualizeExtension.InstallVisualizer(system, new WebApiVisualizer());
            });
        },
            ReactiveServicesOption.WithApi | ReactiveServicesOption.WithSwagger | ReactiveServicesOption.WithTopics
        );

    }

    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {
        app.UseCors(x => x.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
        //app.UseMiddleware<HttpStatusCodeExceptionMiddleware>();
        app.UseReactiveServices();
    }
}
