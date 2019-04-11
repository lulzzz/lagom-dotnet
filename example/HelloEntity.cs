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

/// <summary>
/// This entity is a collection of command handlers, event handlers and
/// behavioural changes in response to the commands and events.
/// </summary>
/// <typeparam name="HelloCommand"></typeparam>
/// <typeparam name="HelloEvent"></typeparam>
/// <typeparam name="HelloState"></typeparam>
public class HelloEntity : ShardedEntity<HelloCommand, HelloEvent, HelloState>
{
    /// <summary>
    /// Prepare the initial behavior for the `HelloEntity` Command cycle.
    /// This gets called on actor initialization before it receives any events.
    /// At this point, all we know about the entity is its `EntityId` which
    /// is provided to us via the `ShardedEntityRegistry.RefFor` method
    /// from within the `HelloServiceImpl`.
    /// </summary>
    /// <param name="state"></param>
    /// <returns></returns>
    public override Behavior InitialBehavior(Option<HelloState> state)
    {
        // Provide an initial state to the entity, which in this case is just
        // a default `HelloState`.  But, it could be anything we choose - including
        // a state ingested from a remote or upstream system.
        return NewBehaviorBuilder(new HelloState())

            // Handler for the `SayHelloCommand` which is read-only and does not
            // persist any events, nor does it change the `State`.
            .SetReadOnlyCommandHandler<SayHelloCommand, string>(
                (cmd, ctx) => ctx.Reply<string>($"{State.Greeting}, {cmd.Name}!")
            )

            // Handler for the `UpdateGreetingCommand` which persists an event
            // to the EventJournal.  It is subsequently handled by the event
            // handler shown next.  We >MUST< respond to the HelloServiceImpl
            // caller from within this handler.  It is done so by calling
            // `ctx.Reply(...)` with an object of the `TR` type denoted
            // by the IReplyType<TR> interface attached to the corresponding
            // `UpdateGreeingCommand`.  We may call `ctx.Reply(...)` either
            // from the callback directly, or from within the side effect
            // callback of the `ThenPersist(...)` method.  These options will
            // respond accordingly:
            //
            //   - within callback ->   may respond before the event is persisted
            //   - as side-effect  ->   will respond after the event is persisted
            //
            // When responding, the choice is either fire-and-forget or wait.
            // All timeouts are bound by the `wyvern.persistence.ask-timeout`
            // configured timespan.
            .SetCommandHandler<UpdateGreetingCommand, string>(
                (cmd, ctx) =>
                {
                    // This routine persists the event to the event journal
                    // and waits for the handler's response which is handled in
                    // the side effect callback.
                    return ctx.ThenPersist(
                        new GreetingUpdatedEvent(cmd.Message),
                        // Callback here waits for the event to be handled first
                        // then allows us to reply to the caller.
                        (e) => ctx.Reply($"{State.Greeting}, {cmd.Name}")
                    );
                }
            )

            // Handler for the `GreetingUpdatedEvent` which is bound to replying
            // with the given state.
            .SetEventHandler<GreetingUpdatedEvent, HelloState>(
                // Handle the event which has been read from the EventJournal.
                // In this case, we're just updating the state and replying
                // with the new state.
                (e) => State.WithGreeting(e.Message)
            )

            // Build the behavior
            .Build();

    }
}