using System;
using System.Collections.Generic;
using System.IO;
using Akka.Actor;
using Akka.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace wyvern.api.ioc
{
    public class ReactiveServicesBuilder : IReactiveServicesBuilder
    {
        private List<Action<IServiceCollection>> ServiceDelegates { get; } = new List<Action<IServiceCollection>>();
        private List<(Type, Type)> TypeMapping { get; } = new List<(Type, Type)>();
        private List<Action<ActorSystem>> ActorSystemDelegates { get; } = new List<Action<ActorSystem>>();

        public ReactiveServicesBuilder()
        {
            ServiceDelegates.Add(x =>
            {
                var akka_type = Environment.GetEnvironmentVariable("AKKA_TYPE");
                if (String.IsNullOrEmpty(akka_type))
                    akka_type = "seed";
                var configakka = ConfigurationFactory.ParseString(File.ReadAllText($"akka.{akka_type}.conf"));
                var actorSystem = ActorSystem.Create("ClusterSystem", configakka);
                x.AddSingleton<ActorSystem>(actorSystem);
                foreach (var actorSystemDelegate in ActorSystemDelegates)
                    actorSystemDelegate.Invoke(actorSystem);
            });
        }

        public ReactiveServicesBuilder AddReactiveService<T, TI>()
            where TI : T
            where T : Service
        {
            ServiceDelegates.Add(x => x.AddTransient<T, TI>());
            TypeMapping.Add((typeof(T), typeof(TI)));
            return this;
        }

        public ReactiveServicesBuilder AddActorSystemDelegate(Action<ActorSystem> actorSystemDelegate)
        {
            ActorSystemDelegates.Add(actorSystemDelegate);
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
