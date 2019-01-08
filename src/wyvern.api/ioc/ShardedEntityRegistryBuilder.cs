using System;
using System.Collections.Generic;
using System.IO;
using Akka.Actor;
using Akka.Configuration;
using wyvern.api.@internal.sharding;
using wyvern.entity.command;
using wyvern.entity.@event;
using wyvern.entity.@event.aggregate;
using wyvern.entity.state;
using System.Threading.Tasks;
using static ClusterDistributionExtensionProvider;

namespace wyvern.api.ioc
{
    public sealed class ShardedEntityRegistryBuilder : IShardedEntityRegistryBuilder
    {
        private List<Action<IShardedEntityRegistry>> RegistryDelegates { get; } =
            new List<Action<IShardedEntityRegistry>>();
        private List<Action<ReadSide>> ReadSideDelegates { get; } = new List<Action<ReadSide>>();
        private List<Action<ActorSystem>> ExtensionDelegates { get; } = new List<Action<ActorSystem>>();

        public ShardedEntityRegistryBuilder()
        {
            ExtensionDelegates.Add(x => x.WithExtension<ClusterDistribution, ClusterDistributionExtensionProvider>());
        }

        public IShardedEntityRegistryBuilder WithShardedEntity<T, C, E, S>()
            where T : ShardedEntity<C, E, S>, new()
            where C : AbstractCommand
            where E : AbstractEvent
            where S : AbstractState
        {
            RegistryDelegates.Add(
                y => y.Register<T, C, E, S>()
            );
            return this;
        }

        public IShardedEntityRegistryBuilder WithReadSide<TE, TP>()
            where TE : AggregateEvent<TE>
            where TP : ReadSideProcessor<TE>, new()
        {
            ReadSideDelegates.Add(
                x => x.Register(() => new TP())
            );
            return this;
        }

        public IShardedEntityRegistryBuilder WithExtension<T, TI>()
            where T : class, IExtension
            where TI : IExtensionId
        {
            ExtensionDelegates.Add(
                y => y.WithExtension<T, TI>()
            );
            return this;
        }

        public IShardedEntityRegistry Build()
        {
            var akka_type = Environment.GetEnvironmentVariable("AKKA_TYPE");
            if (String.IsNullOrEmpty(akka_type))
                akka_type = "seed";
            var configakka = ConfigurationFactory.ParseString(File.ReadAllText($"akka.{akka_type}.conf"));
            var actorSystem = ActorSystem.Create("ClusterSystem", configakka);

            // var commandLineVisualizer = new CommandLineVisualizer();
            // var webVisualizer = new WebApiVisualizer();
            // ActorVisualizeExtension.InstallVisualizer(actorSystem, commandLineVisualizer);
            // ActorVisualizeExtension.InstallVisualizer(actorSystem, webVisualizer);

            foreach (var extensionDelegate in ExtensionDelegates)
                extensionDelegate(actorSystem);

            var registry = new ShardedEntityRegistry(actorSystem);
            foreach (var registryDelegate in RegistryDelegates)
                registryDelegate.Invoke(registry);

            var readsideConfig = new ReadSideConfig(configakka);
            var readside = new ReadSideImpl(
                actorSystem,
                readsideConfig,
                registry
            );

            // TODO: Execution context, materializer

            foreach (var readsideDelegate in ReadSideDelegates)
                readsideDelegate.Invoke(readside);

            // TODO: visualizer
            // Task.Delay(5000)
            //     .ContinueWith((x) =>
            //     {
            //         //webVisualizer.Start();
            //         //commandLineVisualizer.Run();
            //         return Task.CompletedTask;
            //     });

            return registry;
        }
    }
}
