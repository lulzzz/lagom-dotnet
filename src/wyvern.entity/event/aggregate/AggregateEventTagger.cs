using System;

namespace wyvern.entity.@event.aggregate
{
    public class AggregateEventTagger : IAggregateEventTagger
    {
        public AggregateEventTagger(Type eventType)
        {
            EventType = eventType;
        }

        public Type EventType { get; }
    }
}
