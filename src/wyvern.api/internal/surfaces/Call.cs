using System.Reflection;
using wyvern.api.abstractions;

namespace wyvern.api.@internal.surfaces
{
    /// <summary>
    ///     Represents a specific, untyped call
    /// </summary>
    internal abstract class Call : ICall
    {
        public ICallId CallId { get; }
        public MethodInfo MethodRef { get; }
        
        protected Call(CallId callId, MethodInfo methodRef)
        {
            (CallId, MethodRef) =
                (callId, methodRef);
        }
    }

    /// <summary>
    ///     Represents a specific call given a response and request pair
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    internal sealed class Call<TRequest, TResponse> : Call, ICall<TRequest, TResponse>
        where TRequest : class
        where TResponse : class
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="callId">Call identifier</param>
        /// <param name="methodRef">Method reference to service call</param>
        public Call(CallId callId, MethodInfo methodRef)
            : base(callId, methodRef)
        {
        }
    }
}