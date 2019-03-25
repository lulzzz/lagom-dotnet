using wyvern.utils;

namespace wyvern.api.@internal.command
{
    /// <summary>
    /// Envelope for execution of a payload against a given entity
    /// </summary>
    [Immutable]
    internal class CommandEnvelope
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        public CommandEnvelope(string entityId, object payload) =>
            (EntityId, Payload) =
            (entityId, payload);


        /// <summary>
        /// Id of the given entity
        /// </summary>
        /// <value></value>
        public string EntityId { get; }

        /// <summary>
        /// Payload message for execution
        /// </summary>
        /// <value></value>
        public object Payload { get; }
    }
}
