using Akka.Actor;
using Akka.Persistence.Query.Sql;
using Akka.Streams.Util;

namespace wyvern.api.@internal.sharding
{
    internal class SqlServerShardedEntityRegistry : ShardedEntityRegistry
    {
        internal SqlServerShardedEntityRegistry(ActorSystem system)
            : base(system)
        {
        }

        protected override Option<string> QueryPluginId { get; }
            = new Option<string>(SqlReadJournal.Identifier);
    }
}
