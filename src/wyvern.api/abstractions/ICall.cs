using System.Reflection;

namespace wyvern.api
{
    /// <summary>
    ///     Untyped ICall interface
    /// </summary>
    public interface ICall
    {
        ICallId CallId { get; }
        MethodInfo MethodRef { get; }
    }

    /// <summary>
    ///     Interface supplied to support covariance/contravarience
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    public interface ICall<in TRequest, out TResponse> : ICall
    {
    }
}