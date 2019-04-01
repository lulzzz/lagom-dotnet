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
using System.Text;

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
                    if (restCall == null) continue; // SocketCall, PathCall

                    var parameters = Regex.Matches(restCall.PathPattern, "\\{([^\\}]*)\\}")
                            .Select(match => match.Value.Substring(1, match.Value.Length - 2))
                            .Select(x =>
                            {
                                var parts = x.Split(":");
                                var type = parts.Length > 1 ? parts[1] : "string";
                                if (type == "int")
                                {
                                    type = "integer";
                                    // TODO: check min/max values for 64bit
                                }
                                return new NonBodyParameter()
                                {
                                    Name = parts[0],
                                    In = "path",
                                    Required = true,
                                    Type = type,
                                    Format = type == "integer" ? "Int32" : null
                                } as IParameter;
                            })
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
                    var first = call.MethodRef.Name.IndexOf("<get_") + 5;
                    var second = call.MethodRef.Name.IndexOf(">");
                    var method_ref_name = call.MethodRef.Name.Substring(first, second - first);
                    var operation = new Operation()
                    {
                        OperationId = method_ref_name,
                        Consumes = new List<string>() {
                            "application/json"
                        },
                        Produces = new List<string>() {
                            "application/json"
                        },
                        Responses = new Dictionary<string, Response>()
                        {
                            {
                                "200", new Response {
                                    Schema = schemaRegistry.GetOrRegister(resType),
                                    Description = $"Returns {resType.Name}"
                                }
                            }
                        },
                        Tags = new[] { service.Descriptor.Name },
                        Parameters = parameters
                    };

                    Func<String, String> removeTypes = (string str) =>
                        {
                            var sb = new StringBuilder();
                            bool capturing = true;
                            bool thinking = false;
                            foreach (var c in str)
                            {
                                if (thinking)
                                {
                                    if (c == '}')
                                    {
                                        thinking = false;
                                        capturing = true;
                                    }
                                    else if (c == ':')
                                    {
                                        capturing = false;
                                        continue;
                                    }
                                }
                                if (c == '{')
                                    thinking = true;
                                if (capturing)
                                    sb.Append(c);
                            }
                            return sb.ToString();
                        };

                    var newPath = removeTypes(restCall.PathPattern);

                    var exists = swaggerDoc.Paths.ContainsKey(newPath);
                    var path = exists ? swaggerDoc.Paths[newPath] : new PathItem();
                    if (!exists)
                    {
                        swaggerDoc.Paths.Add(
                            newPath,
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
