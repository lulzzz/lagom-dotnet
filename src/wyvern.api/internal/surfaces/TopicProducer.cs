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

        public static Topic<TMessage> SingleStreamWithOffset<TMessage>(Func<Offset, Source<KeyValuePair<TMessage, Offset>, NotUsed>> eventStream)
            where TMessage : class
        {
            return TaggedStreamWithOffset<TMessage>(SingletonTag)
                ((tags, offset) => eventStream.Invoke(offset)
            );
        }

        public static Func<Func<IAggregateEventTag, Offset, Source<KeyValuePair<TMessage, Offset>, NotUsed>>, Topic<TMessage>>
            TaggedStreamWithOffset<TMessage>(ImmutableArray<AggregateEventTag> tags)
            where TMessage : class
        {
            return eventStream =>
                new TaggedOffsetTopicProducer<TMessage>(
                    tags, eventStream
                );
        }

        public sealed class SingletonEvent : AggregateEvent<SingletonEvent>
        {
            public override IAggregateEventTagger AggregateTag => SingletonTag[0];
        }
    }
}
