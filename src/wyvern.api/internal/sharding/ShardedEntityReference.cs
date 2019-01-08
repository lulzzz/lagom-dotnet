using System;
using System.Threading.Tasks;
using Akka.Actor;
using wyvern.api.@internal.command;

namespace wyvern.api.@internal.sharding
{
    internal sealed class ShardedEntityReference : IShardedEntityReference
    {
        internal ShardedEntityReference(
            string entityId,
            IActorRef region,
            ActorSystem actorSystem,
            TimeSpan askTimeout)
        {
            EntityId = entityId;
            Region = region;
            ActorSystem = actorSystem;
            AskTimeout = askTimeout;
        }

        private string EntityId { get; }
        private IActorRef Region { get; }
        private ActorSystem ActorSystem { get; }
        private TimeSpan AskTimeout { get; }

        public async Task<TR> Ask<TR>(IReplyType<TR> command)
        {
            var task = Region.Ask(
                new CommandEnvelope(
                    EntityId,
                    command
                ),
                AskTimeout
            );

            try
            {
                await task;
            }
            catch (Exception ex)
            {
                ActorSystem.Log.Error(ex, "Exception on Ask");
                throw;
            }

            if (task.Exception != null)
                throw task.Exception;

            return (TR) task.Result;
        }
    }
}