using System;
using System.Collections;
using System.Collections.Generic;

namespace wyvern.api.ioc
{
    /// <summary>
    ///     Typed enumerator of reactive services
    /// </summary>
    public class ReactiveServices : IReactiveServices
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        public ReactiveServices(IEnumerable<(Type, Type)> serviceMap)
        {
            ServiceMap.AddRange(serviceMap);
        }

        /// <summary>
        ///     Map of service types to their implementations
        /// </summary>
        private List<(Type, Type)> ServiceMap { get; } = new List<(Type, Type)>();

        /// <inheritdoc />
        /// <summary>
        ///     Get ServiceMap typed enumerator
        /// </summary>
        /// <returns></returns>
        public IEnumerator<(Type, Type)> GetEnumerator()
        {
            return ServiceMap.GetEnumerator();
        }

        /// <inheritdoc />
        /// <summary>
        ///     Get ServiceMap untyped enumerator
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ServiceMap.GetEnumerator();
        }
    }
}