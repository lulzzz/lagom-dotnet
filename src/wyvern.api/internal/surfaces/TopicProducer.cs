using System;
using System.Collections.Immutable;
using Akka;
using Akka.Persistence.Query;
using Akka.Streams.Dsl;
using wyvern.entity.@event.aggregate;

namespace wyvern.api.@internal.surfaces
{
    public class TopicProducer
    {
        private static readonly ImmutableArray<AggregateEventTag> SingletonTag = ImmutableArray.Create(
            AggregateEventTag.Of<SingletonEvent>("singleton")
        );

        public static Topic<TM> SingleStreamWithOffset<TM>(Func<Offset, Source<(TM, Offset), NotUsed>> eventStream)
        {
            return TaggedStreamWithOffset<TM, SingletonEvent>(SingletonTag)
                ((tags, offset) => eventStream.Invoke(offset)
            );
        }

        public static Func<Func<AggregateEventTag, Offset, Source<(TM, Offset), NotUsed>>, Topic<TM>>
            TaggedStreamWithOffset<TM, TE>(ImmutableArray<AggregateEventTag> tags)
        {
            return eventStream =>
                new TaggedOffsetTopicProducer<TM>(
                    tags, eventStream
                );
        }

        public sealed class SingletonEvent : AggregateEvent<SingletonEvent>
        {
            public override IAggregateEventTagger AggregateTag => SingletonTag[0];
        }
    }
}
