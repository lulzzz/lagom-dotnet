using wyvern.entity.state;

public class HelloState : AbstractState
{
    const string DEFAULT_GREETING = "Hello";

    bool Created { get; } = false;

    public string Name { get; }
    public string Greeting { get; } = DEFAULT_GREETING;

    public HelloState() { }

    HelloState(string name, string greeting, bool created)
    {
        Name = name;
        Greeting = greeting;
        Created = created;
    }

    public HelloState WithCreated()
        => new HelloState(Name, Greeting, true);

    public HelloState WithName(string name)
        => new HelloState(name, Greeting, Created);

    public HelloState WithGreeting(string greeting)
        => new HelloState(Name, greeting, Created);

}
