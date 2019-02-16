using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Akka;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.Swagger;
using wyvern.api.@internal.surfaces;
using System.Text.RegularExpressions;
using wyvern.api.abstractions;

namespace wyvern.api.ioc
{
    /// <summary>
    /// Main component responsible for generating swagger documents from
    /// the service descriptors.
    /// </summary>
    public class ReactiveServicesApiDescriptionsDocumentFilter : IDocumentFilter
    {
        IServiceProvider _provider;

        public ReactiveServicesApiDescriptionsDocumentFilter(IServiceProvider provider)
        {
            _provider = provider;
        }

        public void Apply(SwaggerDocument swaggerDoc, DocumentFilterContext context)
        {
            var schemaRegistry = context.SchemaRegistry;
            var reactiveServices = _provider.GetService<IReactiveServices>();
            foreach (var (serviceType, _) in reactiveServices)
            {
                var instance = _provider.GetService(serviceType);
                var service = (Service)instance;
                foreach (var call in service.Descriptor.Calls)
                {
                    var restCall = call.CallId as RestCallId;

                    var parameters = Regex.Matches(restCall.PathPattern, "\\{([^\\}]*)\\}")
                            .Select(match => match.Value.Split(":"))
                            .Select(x => new NonBodyParameter()
                            {
                                Name = x[0].Substring(1, x[0].Length - 2),
                                In = "path"
                                // TODO: Type from split, min / max, required
                            } as IParameter)
                            .ToList();

                    var mref = call.MethodRef;
                    var reqType = mref.ReturnType.GenericTypeArguments[0];

                    if (reqType != typeof(NotUsed))
                    {
                        var reqSchema = schemaRegistry.GetOrRegister(reqType);
                        parameters = parameters.Concat(
                                new IParameter[] {
                                    new BodyParameter
                                    {
                                        Schema = reqSchema,
                                        Name = reqType.Name,
                                        Required = true,
                                        Description = reqType.Name
                                    }
                                }
                            ).ToList();
                    }

                    var resType = mref.ReturnType
                        .GenericTypeArguments[1]  // Task<T>
                        .GenericTypeArguments[0]; // T
                    var operation = new Operation()
                    {
                        OperationId = call.MethodRef.Name,
                        Consumes = new List<string>() {
                            "application/json"
                        },
                        Produces = new List<string>() {
                            "application/json"
                        },
                        Responses = new Dictionary<string, Response>()
                        {
                            { "200", new Response { Schema = schemaRegistry.GetOrRegister(resType) } }
                        },
                        Tags = new[] { service.Descriptor.Name },
                        Parameters = parameters
                    };


                    var exists = swaggerDoc.Paths.ContainsKey(restCall.PathPattern);
                    var path = exists ? swaggerDoc.Paths[restCall.PathPattern] : new PathItem();
                    if (!exists)
                    {
                        swaggerDoc.Paths.Add(
                            restCall.PathPattern,
                            path
                        );
                    }

                    /*
                     * Register each method against the existing path dictionary which also
                     * preforms checking for duplicate paths
                     */
                    if (restCall.Method == Method.DELETE)
                    {
                        if (path.Delete != null) throw new InvalidOperationException("Duplicate path");
                        path.Delete = operation;
                    }
                    else if (restCall.Method == Method.GET)
                    {
                        if (path.Get != null) throw new InvalidOperationException("Duplicate path");
                        path.Get = operation;
                    }
                    else if (restCall.Method == Method.POST)
                    {
                        if (path.Post != null) throw new InvalidOperationException("Duplicate path");
                        path.Post = operation;
                    }
                    else if (restCall.Method == Method.PATCH)
                    {
                        if (path.Patch != null) throw new InvalidOperationException("Duplicate path");
                        path.Patch = operation;
                    }
                    else if (restCall.Method == Method.PUT)
                    {
                        if (path.Put != null) throw new InvalidOperationException("Duplicate path");
                        path.Put = operation;
                    }
                }
            }
        }
    }
}
