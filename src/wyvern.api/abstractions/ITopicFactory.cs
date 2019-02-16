using wyvern.api.@internal.surfaces;

namespace wyvern.api.abstractions
{
    public interface ITopicFactory
    {
        Topic<M> Create<M>(ITopicCall<M> call);
    }
}