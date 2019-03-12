public partial class HelloCommand
{
    public class SayHelloCommand : HelloCommand
    {
        public string Name { get; }
        public SayHelloCommand(string name) => Name = name;
    }
}
