using wyvern.api.@internal.surfaces;

namespace wyvern.api.abstractions
{
    public interface ITopicCall
    {
        ITopicId TopicId { get; }
        object TopicHolder { get; }
    }

    public interface ITopicCall<out M> : ITopicCall
    {
        ITopicCall<M> WithTopicHolder(TopicHolder topicHolder);
    }
}