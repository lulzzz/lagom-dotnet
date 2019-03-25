using wyvern.api.abstractions;
using wyvern.entity.command;

public class HelloCommand : AbstractCommand, IReplyType<string>
{
    public class SayHelloCommand : HelloCommand
    {
        public string Name { get; }
        public SayHelloCommand(string name) => Name = name;
    }

    public HelloCommand() { }

    public class UpdateGreetingCommand : HelloCommand
    {
        public string Name { get; }
        public string Message { get; }
        public UpdateGreetingCommand(string name, string message) => (Name, Message) = (name, message);
    }
}
