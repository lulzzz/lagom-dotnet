using Akka.Visualize;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
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
            /* Register your entities here */
            x.WithShardedEntity<HelloEntity, HelloCommand, HelloEvent, HelloState>();
        });

        services.AddReactiveServices(x =>
            {
                /* Register all the services here */
                x.AddReactiveService<HelloService, HelloServiceImpl>();

                /* Any additions to the actor system can be done in here */
                x.AddActorSystemDelegate(system =>
                {
                    /* This visualizer is a bit of a nice to have, not fully functional yet */
                    //ActorVisualizeExtension.InstallVisualizer(system, new WebApiVisualizer());
                });
            },
            /*
             * Optionally enable any reactive services options here.
             * Note, for any console apps you can simply use `None`
             */
            ReactiveServicesOption.WithApi |
            ReactiveServicesOption.WithSwagger
        );

    }

    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {
        app.UseCors(x => x.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
        //app.UseMiddleware<HttpStatusCodeExceptionMiddleware>();
        app.UseReactiveServices();
    }
}