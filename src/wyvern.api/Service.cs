using System;
using wyvern.api.abstractions;
using wyvern.api.@internal.surfaces;
using wyvern.utils;

namespace wyvern.api
{
    // TODO: Interface and inject.

    /// <summary>
    /// Main entry point for converting requests to calls against various
    /// system endpoints
    /// </summary>
    [Immutable]
    public abstract class Service
    {
        /// <summary>
        /// Descriptor for this service
        /// </summary>
        /// <value></value>
        public abstract IDescriptor Descriptor { get; }

        /// <summary>
        /// Initializes a named descriptor on this service
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected static IDescriptor Named(string name)
        {
            return @internal.surfaces.Descriptor.Named(name);
        }

        /// <summary>
        /// Creates a path call mapping from Request to Response type
        /// </summary>
        /// <param name="pathPattern"></param>
        /// <param name="methodRef"></param>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <returns></returns>
        protected static ICall<TRequest, TResponse> PathCall<TRequest, TResponse>(
            string pathPattern,
            Func<Func<TRequest, TResponse>> methodRef)
            where TRequest : class
            where TResponse : class
        {
            return new Call<TRequest, TResponse>(
                new PathCallId(pathPattern),
                methodRef.Method
            );
        }

        /// <summary>
        /// Creates a path call type from Request to Response type with a single parameter
        /// </summary>
        /// <param name="pathPattern"></param>
        /// <param name="methodRef"></param>
        /// <typeparam name="TA"></typeparam>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <returns></returns>
        protected static ICall<TRequest, TResponse> PathCall<TA, TRequest, TResponse>(
            string pathPattern,
            Func<TA, Func<TRequest, TResponse>> methodRef)
            where TRequest : class
            where TResponse : class
        {
            return new Call<TRequest, TResponse>(
                new PathCallId(pathPattern),
                methodRef.Method
            );
        }

        /// <summary>
        /// Creates a path call type from Request to Response type with two parameters
        /// </summary>
        /// <param name="pathPattern"></param>
        /// <param name="methodRef"></param>
        /// <typeparam name="TA"></typeparam>
        /// <typeparam name="TB"></typeparam>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <returns></returns>
        protected static ICall<TRequest, TResponse> PathCall<TA, TB, TRequest, TResponse>(
            string pathPattern,
            Func<TA, TB, Func<TRequest, TResponse>> methodRef)
            where TRequest : class
            where TResponse : class
        {
            return new Call<TRequest, TResponse>(
                new PathCallId(pathPattern),
                methodRef.Method
            );
        }

        /// <summary>
        /// Creates a path call type from Request to Response type with two parameters
        /// </summary>
        /// <param name="pathPattern"></param>
        /// <param name="methodRef"></param>
        /// <typeparam name="TA"></typeparam>
        /// <typeparam name="TB"></typeparam>
        /// <typeparam name="TC"></typeparam>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <returns></returns>
        protected static ICall<TRequest, TResponse> PathCall<TA, TB, TC, TRequest, TResponse>(
            string pathPattern,
            Func<TA, TB, TC, Func<TRequest, TResponse>> methodRef)
            where TRequest : class
            where TResponse : class
        {
            return new Call<TRequest, TResponse>(
                new PathCallId(pathPattern),
                methodRef.Method
            );
        }

        /// <summary>
        /// Creates a REST call type from Request to Response type
        /// </summary>
        /// <param name="method"></param>
        /// <param name="pathPattern"></param>
        /// <param name="methodRef"></param>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <returns></returns>
        protected static ICall<TRequest, TResponse> RestCall<TRequest, TResponse>(
            Method method,
            string pathPattern,
            Func<Func<TRequest, TResponse>> methodRef)
            where TRequest : class
            where TResponse : class
        {
            return new Call<TRequest, TResponse>(
                new RestCallId(method, pathPattern),
                methodRef.Method
            );
        }

        /// <summary>
        /// Creates a REST call type from Request to Response type with a single
        /// parameter
        /// </summary>
        /// <param name="method"></param>
        /// <param name="pathPattern"></param>
        /// <param name="methodRef"></param>
        /// <typeparam name="TA"></typeparam>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <returns></returns>
        protected static ICall<TRequest, TResponse> RestCall<TA, TRequest, TResponse>(
            Method method,
            string pathPattern,
            Func<TA, Func<TRequest, TResponse>> methodRef)
            where TRequest : class
            where TResponse : class
        {
            if (pathPattern == null) throw new ArgumentNullException(nameof(pathPattern));
            return new Call<TRequest, TResponse>(
                new RestCallId(method, pathPattern),
                methodRef.Method
            );
        }

        /// <summary>
        /// Creates a REST call type from Request to Response type with a double
        /// parameter
        /// </summary>
        /// <param name="method"></param>
        /// <param name="pathPattern"></param>
        /// <param name="methodRef"></param>
        /// <typeparam name="TA"></typeparam>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <returns></returns>
        protected static ICall<TRequest, TResponse> RestCall<TA, TB, TRequest, TResponse>(
            Method method,
            string pathPattern,
            Func<TA, TB, Func<TRequest, TResponse>> methodRef)
            where TRequest : class
            where TResponse : class
        {
            if (pathPattern == null) throw new ArgumentNullException(nameof(pathPattern));
            return new Call<TRequest, TResponse>(
                new RestCallId(method, pathPattern),
                methodRef.Method
            );
        }

        /// <summary>
        /// Creates a REST call type from Request to Response type with a double
        /// parameter
        /// </summary>
        /// <param name="method"></param>
        /// <param name="pathPattern"></param>
        /// <param name="methodRef"></param>
        /// <typeparam name="TA"></typeparam>
        /// <typeparam name="TB"></typeparam>
        /// <typeparam name="TC"></typeparam>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <returns></returns>
        protected static ICall<TRequest, TResponse> RestCall<TA, TB, TC, TRequest, TResponse>(
            Method method,
            string pathPattern,
            Func<TA, TB, TC, Func<TRequest, TResponse>> methodRef)
            where TRequest : class
            where TResponse : class
        {
            if (pathPattern == null) throw new ArgumentNullException(nameof(pathPattern));
            return new Call<TRequest, TResponse>(
                new RestCallId(method, pathPattern),
                methodRef.Method
            );
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="topicId"></param>
        /// <param name="methodRef"></param>
        /// <typeparam name="TM"></typeparam>
        /// <returns></returns>
        protected static ITopicCall<TM> Topic<TM>(string topicId, Func<Topic<TM>> methodRef)
        {
            return new TopicCall<TM>(
                new TopicId(topicId),
                new MethodTopicHolder(methodRef.Method)
            );
        }

        /// <summary>
        /// Stream call using websockets
        /// </summary>
        /// <param name="method"></param>
        /// <param name="pathPattern"></param>
        /// <param name="Func<Func<TRequest"></param>
        /// <param name="methodRef"></param>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <returns></returns>
        protected static ICall<TRequest, TResponse> StreamCall<TRequest, TResponse>(
            string pathPattern,
            Func<Func<TRequest, TResponse>> methodRef)
            where TRequest : class
            where TResponse : class
        {
            return new Call<TRequest, TResponse>(
                new StreamCallId(pathPattern),
                methodRef.Method
            );
        }

        /// <summary>
        /// Stream call using websockets
        /// </summary>
        /// <param name="method"></param>
        /// <param name="pathPattern"></param>
        /// <param name="Func<Func<TRequest"></param>
        /// <param name="methodRef"></param>
        /// <typeparam name="TRequest"></typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <returns></returns>
        protected static ICall<TRequest, TResponse> StreamCall<TA, TRequest, TResponse>(
            string pathPattern,
            Func<TA, Func<TRequest, TResponse>> methodRef)
            where TRequest : class
            where TResponse : class
        {
            return new Call<TRequest, TResponse>(
                new StreamCallId(pathPattern),
                methodRef.Method
            );
        }
    }
}
