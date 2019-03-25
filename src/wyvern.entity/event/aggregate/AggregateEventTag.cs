using System;
using wyvern.utils;

namespace wyvern.entity.@event.aggregate
{
    public sealed class AggregateEventTag : AggregateEventTagger, IAggregateEventTag
    {
        /// <summary>
        /// Delimiter between base tag and shard id
        /// </summary>
        private const string Separator = "|";

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        public AggregateEventTag(Type eventType, string tag) : base(eventType)
        {
            Tag = tag;
        }

        /// <summary>
        /// Event tag
        /// </summary>
        /// <value></value>
        public string Tag { get; }

        // TODO: Confirm Separator is required or not???
        public static string ShardTag(string baseTagName, string shardId)
        {
            return $"{baseTagName}{Separator}{shardId}";
        }

        public static string SelectShard(int numShards, string entityId)
        {
            return entityId.ToShardId(numShards);
        }

        public static AggregateEventShards Sharded<E>(string baseTag, int numShards)
        {
            return new AggregateEventShards(typeof(E), baseTag, numShards);
        }

        /// <summary>
        /// Instantiate a tag of the given type
        /// </summary>
        /// <typeparam name="E"></typeparam>
        /// <returns></returns>
        public static AggregateEventTag Of<E>(string name = null)
        {
            return new AggregateEventTag(typeof(E), name ?? typeof(E).Name);
        }

        // TODO: equals and hashcode
    }
}
