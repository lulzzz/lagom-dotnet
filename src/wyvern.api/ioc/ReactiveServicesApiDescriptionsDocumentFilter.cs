using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Akka;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.Swagger;
using wyvern.api.@internal.surfaces;
using System.Text.RegularExpressions;

namespace wyvern.api.ioc
{

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
                    var mrefParams = mref.GetParameters().Select(x => x.Name);
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

                    if (restCall.Method == Method.POST)
                    {
                        if (path.Post != null) throw new InvalidOperationException("Duplicate path");
                        path.Post = operation;
                    }
                    else if (restCall.Method == Method.GET)
                    {
                        if (path.Get != null) throw new InvalidOperationException("Duplicate path");
                        path.Get = operation;
                    }
                }
            }
        }
    }
}
