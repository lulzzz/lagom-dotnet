using wyvern.api.abstractions;

namespace wyvern.api.@internal.surfaces
{
    public interface Topic<M>
    {
        ITopicId TopicId { get; }
        Subscriber<M> Subscriber();
    }
}