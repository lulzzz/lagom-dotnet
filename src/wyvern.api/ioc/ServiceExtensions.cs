using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Akka;
using Akka.Actor;
using Akka.Dispatch;
using Akka.Persistence.Query;
using Akka.Streams;
using Akka.Streams.Dsl;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Swagger;
using wyvern.api.abstractions;
using wyvern.api.exceptions;
using wyvern.api.@internal.surfaces;
using wyvern.utils;

namespace wyvern.api.ioc
{
    public static class ServiceExtensions
    {
        [Flags]
        public enum ReactiveServicesOption
        {
            None,
            /// <summary>
            /// Enables the REST API
            /// </summary>
            WithApi,
            /// <summary>
            /// Enables Swagger generation on the REST API
            /// </summary>
            WithSwagger,
            /// <summary>
            /// Enables the visualizer
            /// </summary>
            WithVisualizer,
            /// <summary>
            /// Enables publishing to an AMQP topic
            /// </summary>
            WithTopics
        }

        static ReactiveServicesOption Options;

        /// <summary>
        /// Add the main reactive services components, including swagger generation
        /// </summary>
        /// <param name="services"></param>
        /// <param name="builderDelegate"></param>
        /// <returns></returns>
        public static IServiceCollection AddReactiveServices(this IServiceCollection services,
            Action<ReactiveServicesBuilder> builderDelegate, ReactiveServicesOption options)
        {
            Options = options;

            // Add reactive services core
            var builder = new ReactiveServicesBuilder();
            builderDelegate(builder);
            services.AddSingleton(builder.Build(services));

            // Optionally, expose reactive services via API
            if (Options.HasFlag(ReactiveServicesOption.WithApi))
            {
                // Routing for mapping HTTP URLs in Kestrel
                services.AddRouting();

                // Swagger generation
                if (Options.HasFlag(ReactiveServicesOption.WithSwagger))
                {
                    services.TryAddSingleton<IApiDescriptionGroupCollectionProvider, ReactiveServicesApiDescriptionGroupProvider>();
                    services.AddSwaggerGen(c =>
                    {
                        c.DocumentFilter<ReactiveServicesApiDescriptionsDocumentFilter>();
                        c.SwaggerDoc("v1", new Info()
                        {
                            // TODO: make this name configurable...
                            Title = "My Reactive Services",
                                Version = "v1"
                        });
                    });
                }
            }

            return services;
        }

        public static IApplicationBuilder UseReactiveServices(this IApplicationBuilder app)
        {
            var services = app.ApplicationServices;
            var reactiveServices = services.GetService<IReactiveServices>();

            void ServiceIterator(Action<Service, Type> x)
            {
                foreach (var(serviceType, _) in reactiveServices)
                    x((Service) services.GetService(serviceType), serviceType);
            }

            // Register any service bound topics
            if (Options.HasFlag(ReactiveServicesOption.WithTopics))
            {
                ServiceIterator((service, serviceType) =>
                {
                    foreach (var topic in service.Descriptor.Topics)
                        RegisterTopic(
                            topic,
                            service,
                            services.GetService<ISerializer>(),
                            app.ApplicationServices.GetService<ActorSystem>()
                        );
                });
            }

            app.UseWebSockets();

            // Build the API components
            if (Options.HasFlag(ReactiveServicesOption.WithApi))
            {
                var router = new RouteBuilder(app);

                // Register all calls for the services
                ServiceIterator((service, serviceType) =>
                {
                    foreach (var call in service.Descriptor.Calls)
                    {
                        switch (call.CallId)
                        {
                            case RestCallId r:
                                RegisterCall(router, service, serviceType, call);
                                break;
                            case StreamCallId s:
                                RegisterStream(router, service, serviceType, call, app);
                                break;
                            default:
                                throw new Exception("Unknown call type");
                        }

                    }
                });

                // Visualization components
                if (Options.HasFlag(ReactiveServicesOption.WithVisualizer))
                    AddVisualizer(router);

                // Build the API
                var routes = router.Build();
                app.UseRouter(routes);

                // Optionally, add swagger components
                if (Options.HasFlag(ReactiveServicesOption.WithSwagger))
                {
                    var config = services.GetService<IConfiguration>();
                    var swaggerDocsApiName = config.GetValue<string>("SwaggerDocs:ApiName", "My API V1");

                    app.UseSwagger();
                    app.UseSwaggerUI(x =>
                    {
                        x.SwaggerEndpoint("/swagger/v1/swagger.json", swaggerDocsApiName);
                        x.RoutePrefix = string.Empty;
                    });
                }

            }

            return app;
        }

        /// <summary>
        /// Add a cluster visualizer to the endpoints /api/visualizer/list and
        /// /api/visualizer/send
        /// </summary>
        /// <param name="router"></param>
        private static void AddVisualizer(IRouteBuilder router)
        {
            router.MapGet("/api/visualizer/list", async(req, res, ctx) =>
            {
                req.Query.TryGetValue("path", out var path);
                var obj = await WebApiVisualizer.Root.List(path);
                var jsonString = JsonConvert.SerializeObject(obj);
                byte[] content = Encoding.UTF8.GetBytes(jsonString);
                res.ContentType = "application/json";
                await res.Body.WriteAsync(content, 0, content.Length);
            });

            router.MapGet("/api/visualizer/send", async(req, res, ctx) =>
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

        private static void RegisterTopic(object t, Service s, ISerializer serializer, ActorSystem sys)
        {
            var topicCall = (ITopicCall) t;
            if (!(topicCall.TopicHolder is MethodTopicHolder))
                throw new NotImplementedException();

            // TODO: Offset registry
            // TODO: SenderLink

            var producer = ((MethodTopicHolder) topicCall.TopicHolder).Method.Invoke(s, null);
            typeof(ITaggedOffsetTopicProducer<>)
            .MakeGenericType(producer.GetType().GetGenericArguments() [0])
                .GetMethod("Init")
                .Invoke(producer, new object[] { sys, topicCall.TopicId.Name, serializer });

        }

        /// <summary>
        /// Register a route to the service call
        /// </summary>
        /// <param name="router"></param>
        /// <param name="service"></param>
        /// <param name="serviceType"></param>
        /// <param name="call"></param>
        private static void RegisterCall(IRouteBuilder router, Service service, Type serviceType, ICall call)
        {
            var(routeMapper, path) = ExtractRoutePath(router, call);

            var mref = call.MethodRef;
            var mrefParams = mref.GetParameters();
            var methodRefType = mref.ReturnType;
            var requestType = methodRefType.GenericTypeArguments[0];

            routeMapper(
                path,
                async(req, res, data) =>
                {
                    object[] mrefParamArray = mrefParams.Select(x =>
                        {
                            var type = x.ParameterType;
                            var name = x.Name;
                            try
                            {
                                var val = data.Values[name].ToString();
                                if (type == typeof(String))
                                    return val as object;
                                if (type == typeof(Int64))
                                    return Int64.Parse(val) as object;
                                if (type == typeof(Int32))
                                    return Int32.Parse(val) as object;
                                if (type == typeof(Int16))
                                    return Int16.Parse(val) as object;

                                throw new Exception("Unsupported path parameter type: " + type.Name);
                            }
                            catch (Exception)
                            {
                                throw new Exception($"Failed to match URL parameter [{name}] in path template.");
                            }
                        })
                        .ToArray();

                    // TODO: Casting...

                    var mres = mref.Invoke(service, mrefParamArray);
                    var cref = mres.GetType().GetMethod("Invoke", new [] { requestType });

                    dynamic task;
                    if (requestType == typeof(NotUsed))
                    {
                        task = cref.Invoke(mres, new object[]
                        {
                            NotUsed.Instance
                        });
                    }
                    else
                    {
                        string body;
                        using(var reader = new StreamReader(req.Body, Encoding.UTF8, true, 1024, true))
                        body = reader.ReadToEnd();

                        var obj = JsonConvert.DeserializeObject(body, requestType);
                        task = cref.Invoke(mres, new [] { obj });
                    }

                    try
                    {
                        await task;
                        if (task.Result is Exception)
                            throw task.Result as Exception;
                    }
                    catch (Exception ex)
                    {
                        if (ex is StatusCodeException) throw;
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

        /// <summary>
        /// Register a route to the service call
        /// </summary>
        /// <param name="router"></param>
        /// <param name="service"></param>
        /// <param name="serviceType"></param>
        /// <param name="call"></param>
        private static void RegisterStream(IRouteBuilder router, Service service, Type serviceType, ICall call, IApplicationBuilder app)
        {
            var(_, path) = ExtractRoutePath(router, call);

            var mref = call.MethodRef;
            var mrefParams = mref.GetParameters();
            var methodRefType = mref.ReturnType;

            app.Use(async(context, next) =>
            {
                if (context.Request.Path != path) // TODO: this isn't matching with embedded path variables.
                {
                    await next();
                }
                else
                {
                    if (!context.WebSockets.IsWebSocketRequest)
                        return;

                    object[] mrefParamArray = mrefParams.Select(x =>
                        {
                            var type = x.ParameterType;
                            var name = x.Name;
                            try
                            {
                                var data = context.Request.Query;
                                var val = data[name].ToString();
                                if (type == typeof(String))
                                    return val as object;
                                if (type == typeof(Int64))
                                    return Int64.Parse(val) as object;
                                if (type == typeof(Int32))
                                    return Int32.Parse(val) as object;
                                if (type == typeof(Int16))
                                    return Int16.Parse(val) as object;

                                throw new Exception("Unsupported path parameter type: " + type.Name);
                            }
                            catch (Exception)
                            {
                                throw new Exception($"Failed to match URL parameter [{name}] in path template.");
                            }
                        })
                        .ToArray();

                    var socket = await context.WebSockets.AcceptWebSocketAsync();

                    var mres = mref.Invoke(service, mrefParamArray);
                    var cref = mres.GetType().GetMethod("Invoke");
                    var t = (Task) cref.Invoke(mres, new object[] { socket });
                    await t.ConfigureAwait(false);
                }
            });

        }

        /// <summary>
        /// Extract route path from call identifier
        /// </summary>
        private static(Func<string, Func<HttpRequest, HttpResponse, RouteData, Task>, IRouteBuilder>, string)
        ExtractRoutePath(IRouteBuilder router, ICall call)
        {
            switch (call.CallId)
            {
                case PathCallId pathCallIdentifier:
                    throw new InvalidOperationException("PathCallId path type not set up");

                case RestCallId restCallIdentifier when restCallIdentifier.Method == Method.DELETE:
                    return (router.MapDelete, restCallIdentifier.PathPattern);

                case RestCallId restCallIdentifier when restCallIdentifier.Method == Method.GET:
                    return (router.MapGet, restCallIdentifier.PathPattern);

                case RestCallId restCallIdentifier when restCallIdentifier.Method == Method.PATCH:
                    return ((tmpl, hndlr) => router.MapVerb("PATCH", tmpl, hndlr), restCallIdentifier.PathPattern);

                case RestCallId restCallIdentifier when restCallIdentifier.Method == Method.POST:
                    return (router.MapPost, restCallIdentifier.PathPattern);

                case RestCallId restCallIdentifier when restCallIdentifier.Method == Method.PUT:
                    return (router.MapPut, restCallIdentifier.PathPattern);

                case RestCallId restCallIdentifier:
                    throw new InvalidOperationException("Unhandled REST Method type for RestCallId");

                case StreamCallId streamCallIdentifier:
                    return (null, streamCallIdentifier.PathPattern);

                default:
                    throw new InvalidOperationException("Unknown type");
            }
        }
    }
}