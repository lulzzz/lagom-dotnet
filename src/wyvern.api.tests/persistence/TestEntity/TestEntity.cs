using System;
using System.Collections.Immutable;
using System.Linq;
using Akka.Actor;
using Akka.Persistence;
using Akka.Streams.Util;
using Akka.TestKit;
using wyvern.api.abstractions;
using wyvern.api.ioc;
using wyvern.utils;

namespace wyvern.api.tests
{
    public class TestEntity : ShardedEntity<TestCommand, TestEvent, TestState>
    {
        public class AfterRecovery
        {
            public static AfterRecovery Instance { get; } = new AfterRecovery();
        }

        public class Snapshot
        {
            public static Snapshot Instance { get; } = new Snapshot();
        }

        private ActorSystem ActorSystem { get; }
        private Option<IActorRef> TestProbe { get; }

        public TestEntity(ActorSystem system, Option<IActorRef> probe = new Option<IActorRef>())
        {
            ActorSystem = system;
            TestProbe = probe;
        }

        public enum Mode
        {
            APPEND,
            PREPEND
        }

        public IBehaviorBuilder<TestCommand, TestEvent, TestState> CommonBehavior(Option<TestState> state)
        {
            return NewBehaviorBuilder(state.OrElse(new TestState(Mode.APPEND, new string[] { })))
                .SetReadOnlyCommandHandler<TestCommand.Get, TestState>(
                    (cmd, ctx) => ctx.Reply(State)
                )
                .SetCommandHandler<TestCommand.ChangeMode, TestEvent.ChangedMode>(
                    (cmd, ctx) =>
                    {
                        TestEvent.ChangedMode e;
                        switch (cmd.NextMode)
                        {
                            case Mode.PREPEND:
                                e = new TestEvent.InPrependMode(EntityId);
                                break;
                            case Mode.APPEND:
                                e = new TestEvent.InAppendMode(EntityId);
                                break;
                            default:
                                throw new InvalidOperationException("unknown mode");
                        }

                        return ctx.ThenPersist(e, ctx.Reply);
                    }
                );
        }

        public IBehaviorBuilder<TestCommand, TestEvent, TestState> WithPrependBehavior(Option<TestState> state)
        {
            return CommonBehavior(state)
                .SetCommandHandler<TestCommand.Add, TestEvent.Added>(
                    (cmd, ctx) =>
                    {
                        if (cmd.Count == 1)
                            return ctx.ThenPersist(
                                new TestEvent.Prepended(EntityId, cmd.Text.ToLower()),
                                ctx.Reply
                            );
                        var events = new TestEvent[cmd.Count];
                        for (var i = 0; i < cmd.Count; i++)
                            events[i] = new TestEvent.Prepended(EntityId, cmd.Text.ToLower());
                        return ctx.ThenPersistAll(
                            events.ToImmutableArray(),
                            () => ctx.Reply(events[0])
                        );
                    }
                )
                .SetEventHandler<TestEvent.Prepended, TestState>(
                    e => State.AddText(e.Text)
                )
                .SetEventHandlerChangingBehavior<TestEvent.InAppendMode, TestState>(
                    e => WithAppendBehavior(State.WithMode(Mode.APPEND)).Build()
                );
        }


        public IBehaviorBuilder<TestCommand, TestEvent, TestState> WithAppendBehavior(Option<TestState> state)
        {
            return CommonBehavior(state)
                .SetCommandHandler<TestCommand.Add, TestEvent.Added>(
                    (cmd, ctx) =>
                    {
                        if (cmd.Count == 1)
                            return ctx.ThenPersist(
                                new TestEvent.Appended(EntityId, cmd.Text.ToUpper()),
                                ctx.Reply
                            );
                        var events = new TestEvent[cmd.Count];
                        for (var i = 0; i < cmd.Count; i++)
                            events[i] = new TestEvent.Appended(EntityId, cmd.Text.ToUpper());
                        return ctx.ThenPersistAll(
                            events.ToImmutableArray(),
                            () => ctx.Reply(events[0])
                        );
                    }
                )
                .SetEventHandler<TestEvent.Appended, TestState>(
                    e => State.AddText(e.Text)
                )
                .SetEventHandlerChangingBehavior<TestEvent.InPrependMode, TestState>(
                    e => WithPrependBehavior(State.WithMode(Mode.PREPEND)).Build()
                );
        }

        public override Behavior InitialBehavior(Option<TestState> state)
        {
            if (state.HasValue)
                TestProbe.ForEach(probe => probe.Tell(Snapshot.Instance, ActorRefs.NoSender));

            if (!state.HasValue || state.Value.Mode == Mode.APPEND)
                return WithAppendBehavior(state).Build();
            if (state.Value.Mode == Mode.PREPEND)
                return WithPrependBehavior(state).Build();
            throw new InvalidOperationException("unknown behavior type");
        }


        public override Behavior RecoveryCompleted()
        {
            TestProbe.ForEach(probe =>
            {
                probe.Tell(AfterRecovery.Instance, ActorRefs.NoSender);
            });
            return BehaviorProperty;
        }
    }
}