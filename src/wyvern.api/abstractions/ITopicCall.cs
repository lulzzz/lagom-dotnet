using wyvern.api.@internal.surfaces;

namespace wyvern.api.abstractions
{
    public interface ITopicCall
    {
        TopicId TopicId { get; }
        object TopicHolder { get; }
    }

    public interface ITopicCall<out M> : ITopicCall
    {
        ITopicCall<M> WithTopicHolder(TopicHolder topicHolder);
    }
}