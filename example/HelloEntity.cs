
using System;
using Akka;
using Akka.Streams.Util;
using wyvern.api.abstractions;
using wyvern.api.ioc;
using wyvern.entity.command;
using wyvern.entity.@event;
using wyvern.entity.state;
using static HelloCommand;
using static HelloEvent;

public class HelloEntity : ShardedEntity<HelloCommand, HelloEvent, HelloState>
{
    public override Behavior InitialBehavior(Option<HelloState> state)
    {
        return NewBehaviorBuilder(new HelloState())
            .SetReadOnlyCommandHandler<SayHelloCommand, string>(
                (cmd, ctx) => ctx.Reply<string>($"{State.Greeting}, {cmd.Name}!")
            )
            .SetCommandHandler<UpdateGreetingCommand, string>(
                (cmd, ctx) =>
                {
                    return ctx.ThenPersist(
                        new GreetingUpdatedEvent(cmd.Message),
                        (e) => ctx.Reply($"{State.Greeting}, {cmd.Name}")
                    );
                }
            )
            .SetEventHandler<GreetingUpdatedEvent, HelloState>(
                (e) => State.WithGreeting(e.Message)
            )
            .Build();

    }
}
