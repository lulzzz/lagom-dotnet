using wyvern.api.abstractions;
using wyvern.entity.command;

/// <summary>
/// Command object used to interact with the `HelloEntity`
/// </summary>
/// <remarks>
/// Note: the nested classes are not completely neccessary, but it does help us
/// file things away neatly without needing to worry about namespaces too much.
/// Additionally, the abstract -> sealed keywords help us ensure that the
/// domain model does not leak.
/// </remarks>
public abstract class HelloCommand : AbstractCommand
{
    /// <summary>
    /// Constructor
    /// </summary>
    public HelloCommand() { }

    /// <summary>
    /// Command object designed to request that the Entity response with `Hello, ${name}`
    /// </summary>
    /// <typeparam name="string">Reply type bound to this specific command</typeparam>
    public sealed class SayHelloCommand : HelloCommand, IReplyType<string>
    {
        /// <summary>
        /// Name to be greeted with a specific `Message` (default->Hello)
        /// </summary>
        /// <value></value>
        public string Name { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Name to use in greeting</param>
        public SayHelloCommand(string name) => Name = name;
    }

    /// <summary>
    /// Command object designed to update the greeting message that is used in the
    /// `${message}, ${name}` response.
    /// </summary>
    /// <typeparam name="string">Reply type bound to this specific command</typeparam>
    public sealed class UpdateGreetingCommand : HelloCommand, IReplyType<string>
    {
        /// <summary>
        /// Name to be greeted with a specific `Message`
        /// </summary>
        /// <value></value>
        public string Name { get; }

        /// <summary>
        /// Message used to greet a specific `Name`
        /// </summary>
        /// <value></value>
        public string Message { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Name to be used in the greeting</param>
        /// <param name="message">Message to use to greet a `Name`</param>
        /// <returns></returns>
        public UpdateGreetingCommand(string name, string message) => (Name, Message) = (name, message);
    }
}