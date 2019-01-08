using System;

namespace wyvern.api.@internal.surfaces
{
    public class InternalTopic<Message> : Topic<Message>
    {
        public TopicId TopicId => throw new NotImplementedException();

        // ImmutableArray<AggregateEventTag<Event>> Tags { get; }
        // (AggregateEventTag<Event>, Offset) ReadSideStream { get; } // output...

        // source [ (meesage, offset), _ ]

        public Subscriber<Message> Subscriber()
        {
            throw new NotImplementedException();
        }
    }
}