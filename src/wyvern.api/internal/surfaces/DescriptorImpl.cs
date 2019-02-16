using System.Collections.Immutable;
using wyvern.api.abstractions;
using wyvern.utils;

namespace wyvern.api.@internal.surfaces
{
    /// <inheritdoc />
    /// <summary>
    ///     Descriptor
    /// </summary>
    [Immutable]
    internal class DescriptorImpl : IDescriptor
    {
        /// <summary>
        ///     Descriptor
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal DescriptorImpl(string name)
        {
            (Name, Calls, Topics) =
                (name, ImmutableArray<ICall>.Empty, 
                    ImmutableArray<ITopicCall>.Empty);
        }


        /// <summary>
        ///     Descriptor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="calls"></param>
        /// <param name="topics"></param>
        /// <returns></returns>
        private DescriptorImpl(
            string name,
            ImmutableArray<ICall> calls,
            ImmutableArray<ITopicCall> topics)
        {
            (Name, Calls, Topics) =
                (name, calls, topics);
        }

        /// <summary>
        ///     Name
        /// </summary>
        /// <value></value>
        public string Name { get; }

        /// <summary>
        ///     Calls
        /// </summary>
        /// <value></value>
        public ImmutableArray<ICall> Calls { get; }

        /// <summary>
        ///     Topics
        /// </summary>
        /// <value></value>
        public ImmutableArray<ITopicCall> Topics { get; }

        /// <summary>
        ///     Add an array of calls to the descriptor
        /// </summary>
        /// <param name="calls"></param>
        /// <returns></returns>
        public IDescriptor WithCalls(params ICall[] calls)
        {
            return ReplaceAllCalls(calls);
        }

        /// <inheritdoc />
        /// <summary>
        ///     Add topics to the descriptor
        /// </summary>
        /// <param name="topics"></param>
        /// <returns></returns>
        public IDescriptor WithTopics(ITopicCall[] topics)
        {
            return ReplaceAllTopics(topics);
        }

        /// <summary>
        ///     Add a topic to the descriptor
        /// </summary>
        /// <param name="call"></param>
        /// <returns></returns>
        public IDescriptor WithTopic<M>(ITopicCall<M> call)
        {
            return ReplaceAllTopics(call);
        }

        /// <summary>
        ///     Return a new instance with concatenated calls
        /// </summary>
        /// <param name="calls"></param>
        /// <returns></returns>
        private IDescriptor ReplaceAllCalls(params ICall[] calls)
        {
            return new DescriptorImpl(Name, Calls.AddRange(calls), Topics);
        }

        /// <summary>
        ///     Return a new instance with concatenated topics
        /// </summary>
        /// <param name="topics"></param>
        /// <returns></returns>
        private IDescriptor ReplaceAllTopics(params ITopicCall[] topics)
        {
            return new DescriptorImpl(Name, Calls, Topics.AddRange(topics));
        }
    }
}