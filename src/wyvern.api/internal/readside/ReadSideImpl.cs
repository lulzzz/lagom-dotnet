using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka;
using Akka.Actor;
using Akka.Cluster;
using Akka.Cluster.Sharding;
using Akka.Cluster.Tools.Singleton;
using Akka.Pattern;
using Akka.Streams.Util;
using wyvern.api.abstractions;
using wyvern.utils;

namespace wyvern.api.@internal.readside
{
    internal class ReadSideImpl : ReadSide
    {
        ReadSideConfig Config { get; }
        IShardedEntityRegistry Registry { get; }
        ActorSystem System { get; }

        protected Option<string> Name { get; }

        public ReadSideImpl(ActorSystem system, ReadSideConfig config, IShardedEntityRegistry registry)
        {
            Config = config;
            Registry = registry;
            System = system;
        }

        public override void Register<TE>(Func<ReadSideProcessor<TE>> processorFactory)
        {
            if (!Config.Role.ForAll(Cluster.Get(System).SelfRoles.Contains))
                return;

            ReadSideProcessor<TE> dummyProcessor;
            try
            {
                dummyProcessor = processorFactory();
            }
            catch (Exception)
            {
                throw new InvalidOperationException(
                    "Cannot create instance of ReadSideProcessor"
                );
            }

            var readSideName = (Name.HasValue ? (Name.Value + "-") : "") + dummyProcessor.ReadSideName;
            var encodedReadSideName = WebUtility.UrlEncode(readSideName);
            var tags = dummyProcessor.AggregateTags;
            var entityIds = tags.Select(x => x.Tag);

            var globalPrepareTask = ClusterStartupTask.Apply(
                System,
                $"readSideGlobalPrepare-{encodedReadSideName}",
                () => processorFactory().BuildHandler().GlobalPrepare(),
                Config.GlobalPrepareTimeout,
                Config.Role,
                Config.MinBackoff,
                Config.MaxBackoff,
                Config.RandomBackoffFactor
            );

            var readSideProps = ReadSideActor.Props(
                Config,
                globalPrepareTask,
                Registry.EventStream<TE>,
                processorFactory
            );

            var shardingSettings = ClusterShardingSettings.Create(System).WithRole(Config.Role.OrElse(""));

            new ClusterDistributionExtensionProvider.ClusterDistribution(System).Start(
                readSideName,
                readSideProps,
                entityIds.ToArray(),
                new ClusterDistributionSettings(System, shardingSettings)
            );

        }

    }

    internal class ClusterStartupTaskActor : ReceiveActor
    {
        public sealed class Execute { }

        TimeSpan Timeout { get; }

        public ClusterStartupTaskActor(Func<Task<Done>> task, TimeSpan timeout)
        {
            Receive<Execute>(x =>
            {
                Context.System.Log.Info($"Executing cluster start task {Self.Path.Name}.");
                task().PipeTo(Self);
                Become(Executing(new[] { Sender }.ToList()));
            });
        }

        protected override void PreStart()
        {
            Self.Ask(new Execute(), Timeout).PipeTo(Self);
        }

        private Action Executing(List<IActorRef> outstandingRequests) => () =>
        {
            Receive<Execute>(execute =>
            {
                Become(Executing(outstandingRequests.Append(Sender).ToList()));
            });
            Receive<Done>(done =>
            {
                Context.System.Log.Info($"Cluster start task {Self.Path.Name} done.");
                outstandingRequests.ForEach(requester => requester.Tell(Done.Instance));
                Become(Executed);
            });
            Receive<Failure>(failure =>
            {
                Context.System.Log.Info($"Cluster start task {Self.Path.Name} failed.");
                outstandingRequests.ForEach(requester => requester.Tell(failure));
                throw failure.Exception;
            });
        };

        private void Executed()
        {
            Receive<Execute>(execute =>
            {
                Sender.Tell(Done.Instance);
            });
            Receive<Done>(done => { });
        }

    }

    internal class ClusterStartupTask
    {
        public static ClusterStartupTask Apply(
            ActorSystem system, string taskName,
            Func<Task<Done>> task, TimeSpan taskTimeout,
            Option<string> role, TimeSpan minBackoff,
            TimeSpan maxBackoff, double randomBackoffFactor)
        {
            var startupTaskProps = Akka.Actor.Props.Create(
                () => new ClusterStartupTaskActor(
                    task, taskTimeout
                )
            );
            var backoffProps = BackoffSupervisor.PropsWithSupervisorStrategy(
                startupTaskProps, taskName, minBackoff, maxBackoff, randomBackoffFactor, SupervisorStrategy.StoppingStrategy
            );
            var singletonProps = ClusterSingletonManager.Props(
                backoffProps, PoisonPill.Instance, ClusterSingletonManagerSettings.Create(system)
            );
            var singleton = system.ActorOf(singletonProps, $"{taskName}-singleton");
            var singletonProxy = system.ActorOf(
                ClusterSingletonProxy.Props(
                    singletonManagerPath: singleton.Path.ToStringWithoutAddress(),
                    settings: ClusterSingletonProxySettings.Create(system).WithRole(role.Value)
                ),
                $"{taskName}-singletonProxy"
            );

            return new ClusterStartupTask(singletonProxy);

        }

        IActorRef ActorRef { get; }

        public ClusterStartupTask(IActorRef actorRef) =>
            ActorRef = actorRef;

        public void Execute(IActorRef sender) =>
            ActorRef.Tell(new ClusterStartupTaskActor.Execute());

        public async Task<Done> AskExecute(TimeSpan timeout)
        {
            var result = await ActorRef.Ask(new ClusterStartupTaskActor.Execute());
            return Done.Instance;
        }
    }
}
