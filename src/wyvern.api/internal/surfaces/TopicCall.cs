using wyvern.api.abstractions;

namespace wyvern.api.@internal.surfaces
{
    internal sealed class TopicCall<M> : ITopicCall<M>
    {
        /// <summary>
        /// Topic identifier
        /// </summary>
        /// <value></value>
        public ITopicId TopicId { get; }
        public object TopicHolder { get; }

        internal TopicCall(ITopicId topicId, object topicHolder)
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