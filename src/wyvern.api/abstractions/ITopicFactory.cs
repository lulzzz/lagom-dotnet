using wyvern.api.@internal.surfaces;

namespace wyvern.api
{
    public interface ITopicFactory
    {
        Topic<M> Create<M>(ITopicCall<M> call);
    }
}