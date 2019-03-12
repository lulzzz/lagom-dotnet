using System;
using System.Collections.Immutable;
using Akka;
using Akka.Actor;
using Akka.Persistence.Query;
using Akka.Streams;
using Akka.Streams.Dsl;
using wyvern.entity.@event;
using wyvern.entity.@event.aggregate;

namespace wyvern.api.@internal.surfaces
{
    public interface InternalTopic
    {
    }

    public interface ITaggedOffsetTopicProducer<TMessage> : InternalTopic
        where TMessage : class
    {
        ImmutableArray<AggregateEventTag> Tags { get; }
        Func<AggregateEventTag, Offset, Source<(TMessage, Offset), NotUsed>> ReadSideStream { get; }
        void Init(ActorSystem sys);
    }

    public sealed class TaggedOffsetTopicProducer<TMessage> : InternalTopic<TMessage>, ITaggedOffsetTopicProducer<TMessage>
        where TMessage : class
    {
        public TaggedOffsetTopicProducer(
            ImmutableArray<AggregateEventTag> tags,
            Func<AggregateEventTag, Offset, Source<(TMessage, Offset), NotUsed>> readSideStream)
        {
            (Tags, ReadSideStream) = (tags, readSideStream);
        }

        public Func<AggregateEventTag, Offset, Source<(TMessage, Offset), NotUsed>> ReadSideStream { get; }
        public ImmutableArray<AggregateEventTag> Tags { get; }

        public void Init(ActorSystem sys)
        {
            // TODO: Use offset storage
            foreach (var tag in Tags)
            {
                ReadSideStream
                    .Invoke(tag, Offset.NoOffset())
                    .ViaMaterialized(KillSwitches.Single<(TMessage, Offset)>(), Keep.Right)
                    // .VIA()
                    .ToMaterialized(Sink.Ignore<(TMessage, Offset)>(), Keep.Both)
                    .Run(ActorMaterializer.Create(sys));
            }
        }
    }
}