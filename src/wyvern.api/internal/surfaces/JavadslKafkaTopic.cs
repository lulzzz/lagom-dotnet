using System;
using wyvern.api.abstractions;

namespace wyvern.api.@internal.surfaces
{
    public class JavadslKafkaTopic<M> : Topic<M>, IJavadslKafkaTopic
    {
        public TopicId TopicId => throw new NotImplementedException();

        public Subscriber<M> Subscriber()
        {
            throw new NotImplementedException();
        }
    }
}