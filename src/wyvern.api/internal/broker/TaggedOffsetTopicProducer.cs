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
using static wyvern.api.@internal.surfaces.SqlServerOffsetStore;

namespace wyvern.api.@internal.surfaces
{
    /// <summary>
    /// Topic producer
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public sealed class TaggedOffsetTopicProducer<TEvent>:
        InternalTopic<TEvent>,
        ITaggedOffsetTopicProducer<TEvent>
        where TEvent : AbstractEvent
        {
            /// <summary>
            /// Event source factory
            /// </summary>
            /// <value></value>
            public Func<AggregateEventTag, Offset, Source<KeyValuePair<TEvent, Offset>, NotUsed>> ReadSideStream { get; }

            /// <summary>
            /// Set of tags
            /// </summary>
            /// <value></value>
            public ImmutableArray<AggregateEventTag> Tags { get; }

            /// <summary>
            /// Amqp sender link
            /// </summary>
            /// <value></value>
            SenderLink SenderLink { get; }

            public TaggedOffsetTopicProducer(
                ImmutableArray<AggregateEventTag> tags,
                Func<AggregateEventTag, Offset, Source<KeyValuePair<TEvent, Offset>, NotUsed>> readSideStream)
            {
                (Tags, ReadSideStream) = (tags, readSideStream);
            }

            public void Init(ActorSystem sys, string topicId, ISerializer serializer)
            {
                var config = sys.Settings.Config;
                foreach (var tag in Tags)
                    Producer.StartTaggedOffsetProducer<TEvent>(
                        sys,
                        Tags,
                        new TopicConfig(config),
                        topicId,
                        (string entityId, Offset o) => ReadSideStream.Invoke(tag, o),
                        serializer,
                        new SqlServerOffsetStore(
                            new SqlServerProvider(config).GetconnectionProvider(),
                            new OffsetStoreConfiguration(sys.Settings.Config)
                        )
                    );

            }

        }
}