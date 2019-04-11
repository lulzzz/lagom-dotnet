namespace wyvern.api.abstractions
{
    /// <summary>
    /// Typed reply type interface
    /// </summary>
    /// <remarks>
    /// When used in the context of an `AbstractCommand` it binds the command to the
    /// `TR` type of response.
    /// </remarks>
    /// <typeparam name="TR"></typeparam>
    public interface IReplyType<TR> : IReplyType { }

    /// <summary>
    /// Masked reply type interface
    /// </summary>
    public interface IReplyType { }
}