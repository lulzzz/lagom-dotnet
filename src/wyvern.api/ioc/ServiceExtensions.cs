using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Akka;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Swagger;
using wyvern.api.@internal.surfaces;

namespace wyvern.api.ioc
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddReactiveServices(this IServiceCollection services,
            Action<ReactiveServicesBuilder> builderDelegate)
        {
            var builder = new ReactiveServicesBuilder();
            builderDelegate(builder);
            var reactiveServices = builder.Build(services);
            services.AddSingleton(reactiveServices);
            services.AddRouting();
            services.TryAddSingleton<IApiDescriptionGroupCollectionProvider, ReactiveServicesApiDescriptionGroupProvider>();
            services.AddSwaggerGen(c =>
            {
                c.DocumentFilter<ReactiveServicesApiDescriptionsDocumentFilter>();
                c.SwaggerDoc("v1", new Info() { Title = "My Reactive Services", Version = "v1" });
            });
            return services;
        }

        public static IApplicationBuilder UseReactiveServices(this IApplicationBuilder app)
        {
            var services = app.ApplicationServices;
            var reactiveServices = services.GetService<IReactiveServices>();

            var router = new RouteBuilder(app);

            foreach (var (serviceType, _) in reactiveServices)
            {
                var instance = services.GetService(serviceType);
                var service = (Service)instance;

                foreach (var call in service.Descriptor.Calls)
                    RegisterCall(router, service, serviceType, call);

                foreach (var topic in service.Descriptor.Topics)
                    RegisterTopic(topic);
            }

            AddVisualizer(router);

            var routes = router.Build();
            app.UseRouter(routes);

            var config = services.GetService<IConfiguration>();
            var swaggerDocsApiName = config.GetValue<string>("SwaggerDocs:ApiName", "My API V1");

            app.UseSwagger();
            app.UseSwaggerUI(x =>
            {
                x.SwaggerEndpoint("/swagger/v1/swagger.json", swaggerDocsApiName);
                x.RoutePrefix = string.Empty;
            });

            return app;
        }

        private static void AddVisualizer(IRouteBuilder router)
        {
            router.MapGet("/api/visualizer/list", async (req, res, ctx) =>
            {
                req.Query.TryGetValue("path", out var path);
                var obj = await WebApiVisualizer.Root.List(path);
                var jsonString = JsonConvert.SerializeObject(obj);
                byte[] content = Encoding.UTF8.GetBytes(jsonString);
                res.ContentType = "application/json";
                await res.Body.WriteAsync(content, 0, content.Length);
            });

            router.MapGet("/api/visualizer/send", async (req, res, ctx) =>
            {
                req.Query.TryGetValue("path", out var path);
                req.Query.TryGetValue("messageType", out var messageType);
                var obj = await WebApiVisualizer.Root.Send(path, messageType);
                var jsonString = JsonConvert.SerializeObject(obj);
                byte[] content = Encoding.UTF8.GetBytes(jsonString);
                res.ContentType = "application/json";
                await res.Body.WriteAsync(content, 0, content.Length);
            });
        }

        private static void RegisterTopic(object t)
        {
            MethodInfo method;
            var topicCall = (ITopicCall)t;
            switch (topicCall.TopicHolder)
            {
                case MethodTopicHolder methodRef:
                    switch (methodRef.Method)
                    {
                        case MethodInfo preResolved:
                            method = preResolved;
                            break;

                        default:
                            throw new NotImplementedException();
                    }

                    break;
                default:
                    throw new Exception($"Unknown {t.GetType().Name} type");
            }

            var methodRefType = method.ReturnType;
            var requestType = methodRefType.GenericTypeArguments[0];

            var holder = new MethodTopicHolder(method);

            topicCall.GetType()
                .GetMethod("WithTopicHolder")
                .Invoke(topicCall, new object[] { holder });

            /* Serializers */

        }

        private static void RegisterCall(IRouteBuilder router, Service service, Type serviceType, ICall call)
        {
            var (routeMapper, path) = ExtractRoutePath(router, call);

            var mref = call.MethodRef;
            var mrefParams = mref.GetParameters();
            var mrefParamNames = mrefParams.Select(x => x.Name);
            var methodRefType = mref.ReturnType;
            var requestType = methodRefType.GenericTypeArguments[0];

            routeMapper(
                path,
                async (req, res, data) =>
                {
                    object[] mrefParamArray = mrefParamNames
                        .Select(x => data.Values[x].ToString())
                        .ToArray();

                    var mres = mref.Invoke(service, mrefParamArray);
                    var cref = mres.GetType().GetMethod("Invoke", new[] { requestType });

                    dynamic task;
                    if (requestType == typeof(NotUsed))
                    {
                        task = cref.Invoke(mres, new object[] { NotUsed.Instance });
                    }
                    else
                    {
                        string body;
                        using (var reader = new StreamReader(req.Body, Encoding.UTF8, true, 1024, true))
                            body = reader.ReadToEnd();

                        var obj = JsonConvert.DeserializeObject(body, requestType);
                        task = cref.Invoke(mres, new[] { obj });
                    }

                    try
                    {
                        await task;
                        if (task.Result is Exception)
                            throw task.Result as Exception;
                    }
                    catch (Exception ex)
                    {
                        // TODO: Logger extensions
                        res.StatusCode = 500;
                        var result = task.Result as Exception;
                        var jsonString = JsonConvert.SerializeObject(result.Message);
                        byte[] content = Encoding.UTF8.GetBytes(jsonString);
                        res.ContentType = "application/json";
                        await res.Body.WriteAsync(content, 0, content.Length);
                        return;
                    }

                    {
                        var result = task.Result;

                        var jsonString = JsonConvert.SerializeObject(result);
                        byte[] content = Encoding.UTF8.GetBytes(jsonString);
                        res.ContentType = "application/json";
                        await res.Body.WriteAsync(content, 0, content.Length);
                    }
                }
            );
        }

        private static (Func<string, Func<HttpRequest, HttpResponse, RouteData, Task>, IRouteBuilder>, string)
            ExtractRoutePath(IRouteBuilder router, ICall call)
        {
            switch (call.CallId)
            {
                case PathCallId pathCallIdentifier:
                    throw new InvalidOperationException("PathCallId path type not set up");

                // ReSharper disable once PossibleUnintendedReferenceComparison
                case RestCallId restCallIdentifier when restCallIdentifier.Method == Method.GET:
                    return (router.MapGet, restCallIdentifier.PathPattern);

                // ReSharper disable once PossibleUnintendedReferenceComparison
                case RestCallId restCallIdentifier when restCallIdentifier.Method == Method.POST:
                    return (router.MapPost, restCallIdentifier.PathPattern);

                case RestCallId restCallIdentifier:
                    throw new InvalidOperationException("Unhandled REST Method type for RestCallId");

                default:
                    throw new InvalidOperationException("Unknown type");
            }
        }
    }
}
