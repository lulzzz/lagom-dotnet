using System;
using System.Collections.Generic;

namespace wyvern.entity.@event.aggregate
{
    /// <summary>
    /// Provides a method for sharding aggregate event tags based on the desired number of
    /// shards
    /// </summary>
    public sealed class AggregateEventShards : AggregateEventTagger, IAggregateEventShards
    {
        /// <summary>
        /// Given number of sharts
        /// </summary>
        /// <value></value>
        public int NumShards { get; }

        /// <summary>
        /// Entity event tag
        /// </summary>
        /// <value></value>
        public string Tag { get; }

        /// <summary>
        /// AggregateEventShards constructor
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="tag"></param>
        /// <param name="numShards"></param>
        /// <returns></returns>
        public AggregateEventShards(Type eventType, string tag, int numShards)
            : base(eventType)
            => (Tag, NumShards)
            = (tag, numShards);

        /// <summary>
        /// Get the event tag for a specific entity id
        /// </summary>
        /// <param name="entityId"></param>
        /// <returns></returns>
        public AggregateEventTag ForEntityId(string entityId)
        {
            return new AggregateEventTag(
                EventType,
                AggregateEventTag.ShardTag(
                    Tag,
                    AggregateEventTag.SelectShard(NumShards, entityId)
                )
            );
        }

        /// <summary>
        /// Return an iteration of all shards on the event
        /// </summary>
        /// <typeparam name="E"></typeparam>
        /// <returns></returns>
        public IEnumerable<AggregateEventTag> AllTags<E>()
        {
            for (var i = 0; i < NumShards; i++)
                yield return new AggregateEventTag(
                    typeof(E),
                    AggregateEventTag.ShardTag(Tag, i.ToString())
                );
        }
    }
}
