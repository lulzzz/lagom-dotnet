using wyvern.api.abstractions;

namespace wyvern.api.@internal.surfaces
{
    internal sealed class TopicCall<M> : ITopicCall<M>
    {
        public TopicId TopicId { get; }
        public object TopicHolder { get; }

        // TODO: Important now to add the serializer in here..
        //MessageSerializer
        //Properties

        internal TopicCall(TopicId topicId, object topicHolder)
        {
            (TopicId, TopicHolder) =
                (topicId, topicHolder);
        }

        public ITopicCall<M> WithTopicHolder(TopicHolder topicHolder)
        {
            return new TopicCall<M>(TopicId, topicHolder);
        }
    }
}
