using System.Reflection;

namespace wyvern.api.@internal.surfaces
{
    public class MethodTopicHolder : TopicHolder
    {
        public MethodTopicHolder(MethodInfo method)
        {
            Method = method;
        }

        public MethodInfo Method { get; }

        public Topic<M> Create<M>(Service service)
        {
            return (Topic<M>) Method.Invoke(service, null);
        }
    }
}