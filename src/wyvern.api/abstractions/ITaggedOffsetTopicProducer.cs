using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Akka;
using Akka.Actor;
using Akka.Persistence.Query;
using Akka.Streams.Dsl;
using wyvern.api.@internal.surfaces;
using wyvern.entity.@event.aggregate;

namespace wyvern.api.abstractions
{
    public interface ITaggedOffsetTopicProducer<TMessage> : InternalTopic
        where TMessage : class
    {
        ImmutableArray<AggregateEventTag> Tags { get; }
        Func<AggregateEventTag, Offset, Source<KeyValuePair<TMessage, Offset>, NotUsed>> ReadSideStream { get; }
        void Init(ActorSystem sys, string topicId);
    }
}
