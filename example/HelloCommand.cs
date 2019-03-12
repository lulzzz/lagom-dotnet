using wyvern.api.abstractions;
using wyvern.entity.command;

public partial class HelloCommand : AbstractCommand, IReplyType<string>
{
    public HelloCommand() { }

    public class UpdateGreetingCommand : HelloCommand
    {
        public string Name { get; }
        public string Message { get; }
        public UpdateGreetingCommand(string name, string message) => (Name, Message) = (name, message);
    }
}
