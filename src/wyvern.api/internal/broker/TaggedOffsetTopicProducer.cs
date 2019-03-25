using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
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
    /// <summary>
    /// Topic producer
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public sealed class TaggedOffsetTopicProducer<TMessage> : InternalTopic<TMessage>, ITaggedOffsetTopicProducer<TMessage>
        where TMessage : class
    {
        /// <summary>
        /// Event source factory
        /// </summary>
        /// <value></value>
        public Func<AggregateEventTag, Offset, Source<KeyValuePair<TMessage, Offset>, NotUsed>> ReadSideStream { get; }

        /// <summary>
        /// Set of tags
        /// </summary>
        /// <value></value>
        public ImmutableArray<AggregateEventTag> Tags { get; }

        SenderLink SenderLink { get; }

        public TaggedOffsetTopicProducer(
            ImmutableArray<AggregateEventTag> tags,
            Func<AggregateEventTag, Offset, Source<KeyValuePair<TMessage, Offset>, NotUsed>> readSideStream)
        {
            (Tags, ReadSideStream) = (tags, readSideStream);
        }

        public void Init(ActorSystem sys, string topicId)
        {
            foreach (var tag in Tags)
                Producer.StartTaggedOffsetProducer<TMessage>(
                    sys,
                    Tags,
                    new TopicConfig(sys.Settings.Config),
                    topicId,
                    (string entityId, Offset o) => ReadSideStream.Invoke(tag, o),
                    new InMemoryOffsetStore() // TODO: Use proper offsetstore
                );

        }

    }
}
