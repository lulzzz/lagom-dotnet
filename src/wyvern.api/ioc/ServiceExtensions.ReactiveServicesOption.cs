using System;

namespace wyvern.api.ioc
{
    public static partial class ServiceExtensions
    {
        [Flags]
        public enum ReactiveServicesOption
        {
            None,
            /// <summary>
            /// Enables the REST API
            /// </summary>
            WithApi,
            /// <summary>
            /// Enables Swagger generation on the REST API
            /// </summary>
            WithSwagger,
            /// <summary>
            /// Enables the visualizer
            /// </summary>
            WithVisualizer,
            /// <summary>
            /// Enables publishing to an AMQP topic
            /// </summary>
            WithTopics
        }
    }
}