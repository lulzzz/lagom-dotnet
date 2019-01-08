using System;
using System.Collections.Generic;

namespace wyvern.entity.@event.aggregate
{
    public sealed class AggregateEventShards : AggregateEventTagger, IAggregateEventShards
    {
        public int NumShards { get; }
        public string Tag { get; }

        public AggregateEventShards(Type eventType, string tag, int numShards) : base(eventType)
        {
            (Tag, NumShards) = (tag, numShards);
        }

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
