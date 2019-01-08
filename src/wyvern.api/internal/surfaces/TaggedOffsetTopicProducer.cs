using System;
using System.Collections.Immutable;
using Akka;
using Akka.Persistence.Query;
using Akka.Streams.Dsl;
using wyvern.entity.@event.aggregate;

namespace wyvern.api.@internal.surfaces
{
    public interface InternalTopic
    {
    }

    public interface ITaggedOffsetTopicProducer : InternalTopic
    {
        ImmutableArray<AggregateEventTag> Tags { get; }
    }

    public sealed class TaggedOffsetTopicProducer<Message> : InternalTopic<Message>, ITaggedOffsetTopicProducer
    {
        public TaggedOffsetTopicProducer(
            ImmutableArray<AggregateEventTag> tags,
            Func<AggregateEventTag, Offset, Source<(Message, Offset), NotUsed>> readSideStream)
        {
            (Tags, ReadSideStream) = (tags, readSideStream);
        }

        public Func<AggregateEventTag, Offset, Source<(Message, Offset), NotUsed>> ReadSideStream { get; }
        public ImmutableArray<AggregateEventTag> Tags { get; }
    }
}