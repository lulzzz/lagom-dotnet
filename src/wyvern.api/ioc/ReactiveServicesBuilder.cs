using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Akka.Actor;
using Akka.Configuration;
using Microsoft.Extensions.DependencyInjection;
using wyvern.api.abstractions;
using wyvern.utils;

namespace wyvern.api.ioc
{
    public class ReactiveServicesBuilder : IReactiveServicesBuilder
    {
        static Func<ISerializer> SerializerFactory = () => new DefaultSerializer();
        static Func<IMessagePropertyExtractor> ExtractorFactory = () => new DefaultExtractor();

        private List<Action<IServiceCollection>> ServiceDelegates { get; } = new List<Action<IServiceCollection>>();
        private List < (Type, Type) > TypeMapping { get; } = new List < (Type, Type) > ();
        private List<Action<ActorSystem>> ActorSystemDelegates { get; } = new List<Action<ActorSystem>>();

        public ReactiveServicesBuilder()
        {
            ServiceDelegates.Add(services =>
            {
                // Prepare config root
                var configRoot = ConfigurationFactory.Empty;

                // Load Akka config values from environment variables first
                foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
                {
                    var key = entry.Key as string ?? String.Empty;
                    if (!key.ToUpper().StartsWith("AKKA:")) continue;
                    configRoot.WithFallback(ConfigurationFactory.ParseString(
                        $"{key.Replace(":", ".")} = {entry.Value as string ?? String.Empty}"
                    ));
                }

                // Load Fallback sources (environment first, then base config)
                var environment = Environment.GetEnvironmentVariable("AKKA_ENVIRONMENT");
                var config = (new []
                    {
                        (1, "akka.conf"), // Last fallback
                        (2, "akka.overrides.conf"), // First fallback
                        (3, $"akka.{environment}.conf") // First preference
                    })
                    .Where(t => (File.Exists(t.Item2)))
                    .OrderByDescending(t => t.Item1)
                    .Aggregate(
                        configRoot,
                        (acc, cur) => acc.WithFallback(File.ReadAllText(cur.Item2))
                    );
                var name = config.GetString("wyvern.cluster-system-name", "ClusterSystem");
                var actorSystem = ActorSystem.Create(name, config);
                services.AddSingleton<ActorSystem>(actorSystem);
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

        public ReactiveServicesBuilder WithTopicSerializer<T>(Func<ISerializer> serializerFactory = null)
        where T : ISerializer, new()
        {
            if (serializerFactory != null)
                SerializerFactory = serializerFactory;
            return this;
        }

        public ReactiveServicesBuilder WithMessagePropertyExtractor<T>(Func<IMessagePropertyExtractor> extractorFactory = null)
        where T : ISerializer, new()
        {
            if (ExtractorFactory != null)
                ExtractorFactory = extractorFactory;
            return this;
        }

        internal IReactiveServices Build(IServiceCollection services)
        {
            ServiceDelegates.Add(x => x.AddTransient<ISerializer>(y => SerializerFactory.Invoke()));
            foreach (var serviceDelegate in ServiceDelegates)
                serviceDelegate.Invoke(services);
            return new ReactiveServices(TypeMapping);
        }
    }
}