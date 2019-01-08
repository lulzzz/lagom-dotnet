using System.Collections.Immutable;

namespace wyvern.api
{
    /// <summary>
    ///     Descriptor interface
    /// </summary>
    [Immutable]
    public interface IDescriptor
    {
        /// <summary>
        ///     Name
        /// </summary>
        /// <value></value>
        string Name { get; }

        /// <summary>
        ///     Calls
        /// </summary>
        /// <value></value>
        ImmutableArray<ICall> Calls { get; }

        /// <summary>
        ///     Topics
        /// </summary>
        /// <value></value>
        ImmutableArray<ITopicCall> Topics { get; }

        /// <summary>
        ///     Add calls to the descriptor
        /// </summary>
        /// <param name="calls"></param>
        /// <returns></returns>
        IDescriptor WithCalls(params ICall[] calls);

        /// <summary>
        ///     Add topics to the descriptor
        /// </summary>
        /// <param name="topics"></param>
        /// <returns></returns>
        IDescriptor WithTopics(params ITopicCall[] topics);
    }
}