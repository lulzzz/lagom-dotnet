using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
                    var mref = call.MethodRef;
                    var mrefParams = mref.GetParameters().Select(x => x.Name);
                    var reqType = mref.ReturnType.GenericTypeArguments[0];
                    var reqSchema = schemaRegistry.GetOrRegister(reqType);

                    var parameters = Regex.Matches(restCall.PathPattern, "\\{([^\\}]*)\\}")
                            .Select(match => match.Value.Split(":"))
                            .Select(x => new NonBodyParameter()
                            {
                                Name = x[0].Substring(1, x[0].Length - 2),
                                In = "path"
                                // TODO: Type from split, min / max, required
                            })
                            .Concat(
                                new IParameter[] {
                                    new BodyParameter
                                    {
                                        Schema = reqSchema,
                                        Name = reqType.Name,
                                        Required = true,
                                        Description = reqType.Name
                                    }
                                }
                            )
                            .ToList();

                    swaggerDoc.Paths.Add(
                        $"{restCall.PathPattern}",
                        new PathItem
                        {
                            Post = restCall.Method != Method.POST ? null :
                                new Operation()
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
                                        { "200", new Response { Schema = schemaRegistry.GetOrRegister(typeof(string))}}
                                    },
                                    Parameters = parameters
                                },
                            Get = restCall.Method != Method.GET ? null :
                                new Operation()
                                {
                                    OperationId = call.MethodRef.Name
                                }
                        }
                    );
                }
            }
        }
    }
}
