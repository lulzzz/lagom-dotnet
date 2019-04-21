using System;
using System.Linq;
using Akka.Actor;

namespace wyvern.api.@internal.readside
{
    internal class EnsureActiveActor : ReceiveActor
    {
        public sealed class Terminated
        {

        }

        public sealed class Tick : DeadLetterSupression
        {
            public static Tick Instance { get; } = new Tick();
        }

        public static Props Props(string[] entityIds, IActorRef shardRegion, TimeSpan ensureActiveInterval)
        {
            return Akka.Actor.Props.Create(
                () => new EnsureActiveActor(entityIds, shardRegion, ensureActiveInterval)
            );
        }

        public String[] EntityIds { get; }
        public IActorRef ShardRegion { get; }
        public TimeSpan EnsureActiveInterval { get; }
        public ICancelable Repeater { get; }

        public EnsureActiveActor(string[] entityIds, IActorRef shardRegion, TimeSpan ensureActiveInterval)
        {
            EntityIds = entityIds;
            ShardRegion = shardRegion;
            EnsureActiveInterval = ensureActiveInterval;

            Repeater = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(
                TimeSpan.FromSeconds(0),
                EnsureActiveInterval,
                Self,
                Tick.Instance,
                Self
            );

            Context.Watch(ShardRegion);

            Receive<Tick>(x =>
            {
                foreach (var entityId in (EntityIds ?? new string[] { }))
                    shardRegion.Tell(new ClusterDistributionExtensionProvider.EnsureActive(entityId));
            });

            Receive<Terminated>(x =>
            {
                Context.Stop(Self);
            });

        }

        protected override void PostStop()
        {
            Repeater.Cancel();
        }

    }
}
