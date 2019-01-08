namespace wyvern.api.@internal.surfaces
{
    public interface Topic<M>
    {
        TopicId TopicId { get; }
        Subscriber<M> Subscriber();
    }
}