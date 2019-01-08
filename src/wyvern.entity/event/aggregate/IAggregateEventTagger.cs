using System;

namespace wyvern.entity.@event.aggregate
{
    public interface IAggregateEventTagger
    {
        Type EventType { get; }
    }
}
