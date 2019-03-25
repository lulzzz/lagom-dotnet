using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using Akka;
using Akka.Actor;
using Akka.Persistence.Query;
using Akka.Streams;
using Akka.Streams.Dsl;
using Amqp;
using Amqp.Framing;
using Amqp.Sasl;
using Address = Amqp.Address;
using wyvern.entity.@event;
using wyvern.entity.@event.aggregate;
using Newtonsoft.Json;
using static Producer;
using wyvern.api.abstractions;

namespace wyvern.api.@internal.surfaces
{
    public class InMemoryOffsetDao : IOffsetDao
    {
        public Offset LoadedOffset { get; private set; }

        public Task<Done> SaveOffset(Offset o)
        {
            LoadedOffset = o;
            return Task.FromResult(Done.Instance);
        }
    }

    public class InMemoryOffsetStore : IOffsetStore
    {
        public Task<IOffsetDao> Prepare(string processorId, string tag)
        {
            return Task.FromResult<IOffsetDao>(new InMemoryOffsetDao());
        }
    }


    public interface InternalTopic
    {
    }

    public interface ITaggedOffsetTopicProducer<TMessage> : InternalTopic
        where TMessage : class
    {
        ImmutableArray<AggregateEventTag> Tags { get; }
        Func<AggregateEventTag, Offset, Source<KeyValuePair<TMessage, Offset>, NotUsed>> ReadSideStream { get; }
        void Init(ActorSystem sys, string topicId);
    }

    public sealed class TaggedOffsetTopicProducer<TMessage> : InternalTopic<TMessage>, ITaggedOffsetTopicProducer<TMessage>
        where TMessage : class
    {
        public TaggedOffsetTopicProducer(
            ImmutableArray<AggregateEventTag> tags,
            Func<AggregateEventTag, Offset, Source<KeyValuePair<TMessage, Offset>, NotUsed>> readSideStream)
        {
            (Tags, ReadSideStream) = (tags, readSideStream);
        }

        public Func<AggregateEventTag, Offset, Source<KeyValuePair<TMessage, Offset>, NotUsed>> ReadSideStream { get; }
        public ImmutableArray<AggregateEventTag> Tags { get; }

        SenderLink SenderLink { get; }

        public void Init(ActorSystem sys, string topicId)
        {
            foreach (var tag in Tags)
                Producer.StartTaggedOffsetProducer<TMessage>(
                    sys,
                    Tags,
                    new TopicConfig(sys.Settings.Config),
                    topicId,
                    (string entityId, Offset o) => ReadSideStream.Invoke(tag, o),
                    new InMemoryOffsetStore()
                );

        }

    }
}
