using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Akka.Actor;
using Akka.Configuration;
using Akka.Event;
using Microsoft.Extensions.DependencyInjection;
using wyvern.api.abstractions;
using wyvern.utils;

namespace wyvern.api.ioc
{
    internal class ReactiveServicesBuilder : IReactiveServicesBuilder
    {
        static Func<ISerializer> SerializerFactory = () => new DefaultSerializer();
        static Func<IMessagePropertyExtractor> ExtractorFactory = () => new DefaultExtractor();

        private List<Action<IServiceCollection>> ServiceDelegates { get; } = new List<Action<IServiceCollection>>();
        private List<(Type, Type)> TypeMapping { get; } = new List<(Type, Type)>();
        private List<Action<ActorSystem>> ActorSystemDelegates { get; } = new List<Action<ActorSystem>>();

        public ReactiveServicesBuilder()
        {
            ServiceDelegates.Add(services =>
            {
                // Format logging
                StandardOutLogger.InfoColor = ConsoleColor.Cyan;
                StandardOutLogger.DebugColor = ConsoleColor.DarkGray;

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
                var config = (new[]
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

                services.AddSingleton<Config>(config);

                var actorSystem = ActorSystem.Create(name, config);
                services.AddSingleton<ActorSystem>(actorSystem);
                foreach (var actorSystemDelegate in ActorSystemDelegates)
                    actorSystemDelegate.Invoke(actorSystem);
            });
        }

        public IReactiveServicesBuilder AddReactiveService<T, TI>()
            where TI : T
            where T : Service
        {
            ServiceDelegates.Add(x => x.AddTransient<T, TI>());
            TypeMapping.Add((typeof(T), typeof(TI)));
            return this;
        }

        public IReactiveServicesBuilder AddActorSystemDelegate(Action<ActorSystem> actorSystemDelegate)
        {
            ActorSystemDelegates.Add(actorSystemDelegate);
            return this;
        }

        public IReactiveServicesBuilder WithTopicSerializer<T>(Func<ISerializer> serializerFactory = null)
        where T : ISerializer, new()
        {
            if (serializerFactory != null)
                SerializerFactory = serializerFactory;
            return this;
        }

        public IReactiveServicesBuilder WithMessagePropertyExtractor<T>(Func<IMessagePropertyExtractor> extractorFactory = null)
        where T : IMessagePropertyExtractor, new()
        {
            if (extractorFactory != null)
                ExtractorFactory = extractorFactory;
            return this;
        }

        internal IReactiveServices Build(IServiceCollection services)
        {
            ServiceDelegates.Add(x => x.AddTransient<ISerializer>(y => SerializerFactory.Invoke()));
            ServiceDelegates.Add(x => x.AddTransient<IMessagePropertyExtractor>(y => ExtractorFactory.Invoke()));
            foreach (var serviceDelegate in ServiceDelegates)
                serviceDelegate.Invoke(services);
            return new ReactiveServices(TypeMapping);
        }
    }
}