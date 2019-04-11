using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Akka;
using Akka.Persistence.Query;
using Akka.Streams.Dsl;
using wyvern.entity.@event;
using wyvern.entity.@event.aggregate;

namespace wyvern.api.@internal.surfaces
{
    public class TopicProducer
    {
        private static readonly ImmutableArray<AggregateEventTag> SingletonTag = ImmutableArray.Create(
            AggregateEventTag.Of<SingletonEvent>("singleton")
        );

        public static Topic<TEvent> SingleStreamWithOffset<TEvent>(Func<Offset, Source<KeyValuePair<TEvent, Offset>, NotUsed>> eventStream)
        where TEvent : AbstractEvent
        {
            return TaggedStreamWithOffset<TEvent>(SingletonTag)
                ((tags, offset) => eventStream.Invoke(offset));
        }

        public static Func<Func<IAggregateEventTag, Offset, Source<KeyValuePair<TEvent, Offset>, NotUsed>>, Topic<TEvent>> TaggedStreamWithOffset<TEvent>(ImmutableArray<AggregateEventTag> tags)
        where TEvent : AbstractEvent
        {
            return eventStream =>
                new TaggedOffsetTopicProducer<TEvent>(
                    tags, eventStream
                );
        }

        public sealed class SingletonEvent : AggregateEvent<SingletonEvent>
        {
            public override IAggregateEventTagger AggregateTag => SingletonTag[0];
        }
    }
}