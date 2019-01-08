using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace wyvern.api.ioc
{
    public class ReactiveServicesBuilder : IReactiveServicesBuilder
    {
        private List<Action<IServiceCollection>> ServiceDelegates { get; } = new List<Action<IServiceCollection>>();
        private List<(Type, Type)> TypeMapping { get; } = new List<(Type, Type)>();

        public ReactiveServicesBuilder AddReactiveService<T, TI>()
            where TI : T
            where T : Service
        {
            ServiceDelegates.Add(x => x.AddTransient<T, TI>());
            TypeMapping.Add((typeof(T), typeof(TI)));
            return this;
        }

        internal IReactiveServices Build(IServiceCollection services)
        {
            foreach (var serviceDelegate in ServiceDelegates)
                serviceDelegate.Invoke(services);
            return new ReactiveServices(TypeMapping);
        }
    }
}
