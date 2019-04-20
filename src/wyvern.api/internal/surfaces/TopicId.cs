using wyvern.api.abstractions;

namespace wyvern.api.@internal.surfaces
{
    internal class TopicId : ITopicId
    {
        public TopicId(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}