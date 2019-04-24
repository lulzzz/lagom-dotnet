using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Akka;
using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Event;
using Akka.Streams.Util;
using Akka.TestKit;
using Akka.TestKit.Xunit;
using Google.Protobuf.WellKnownTypes;
using wyvern.api.@internal.sharding;
using wyvern.utils;
using Xunit;

namespace wyvern.api.tests
{
    public class PersistentEntityActorTests : TestKit, IClassFixture<ActorSystemFixture>
    {
        public class PassivatingActor : ReceiveActor
        {
            public IActorRef Child { get; }

            public PassivatingActor()
            {
                Child = Context.ActorOf(ShardedEntityActorProps.Create<TestEntity, TestCommand, TestEvent, TestState>(
                        "test",
                        OptionInitializers.Some("1"),
                        () => new TestEntity(Context.System),
                        100,
                        1d.seconds(),
                        "",
                        ""
                    )
                );
                Receive<Passivate>(stopMsg => Sender.Tell(stopMsg.StopMessage));
                ReceiveAny(x => Child.Forward(x));
            }
        }

        public ActorSystem ActorSystem { get; }

        public PersistentEntityActorTests(ActorSystemFixture actorSystem)
        {
            ActorSystem = actorSystem.ActorSystem;
        }

        [Fact]
        public void save_snapshots()
        {
            {
                var probe = CreateTestProbe();
                var p = ActorSystem.ActorOf(ShardedEntityActorProps.Create<TestEntity, TestCommand, TestEvent, TestState>(
                    "test",
                    OptionInitializers.Some("4"),
                    () => new TestEntity(ActorSystem),
                    3,
                    10d.seconds(),
                    "",
                    ""
                ));

                ActorSystem.EventStream.Subscribe(probe.Ref, typeof(UnhandledMessage));

                for (var i = 0; i < 10; i++)
                {
                    p.Tell(new TestCommand.Add($"{i}"));
                    ExpectMsg(new TestEvent.Appended("4", $"{i}"));
                }

                probe.ExpectNoMsg(TimeSpan.FromMilliseconds(300));
                ActorSystem.EventStream.Unsubscribe(probe.Ref);
            }

            Within(10.0d.seconds(), () =>
            {
                AwaitAssert(() =>
                {
                    var probe = CreateTestProbe();
                    var p = ActorSystem.ActorOf(
                        ShardedEntityActorProps.Create<TestEntity, TestCommand, TestEvent, TestState>(
                            "test",
                            OptionInitializers.Some("4"),
                            () => new TestEntity(ActorSystem, new Option<IActorRef>(probe.Ref)),
                            3,
                            10d.seconds(),
                            "",
                            ""
                        ));

                    probe.ExpectMsg<TestEntity.Snapshot>();
                    p.Tell(new TestCommand.Get());
                    var state = ExpectMsg<TestState>();
                    var words = state.Words.ToList();
                    Assert.Equal(Enumerable.Range(0, 10).Select(x => x.ToString()).ToList(), words);
                });
            });

        }

        [Fact]
        public void persist_multiple_events_on_single_command()
        {
            var p = ActorSystem.ActorOf(ShardedEntityActorProps.Create<TestEntity, TestCommand, TestEvent, TestState>(
                "test",
                OptionInitializers.Some("5"),
                () => new TestEntity(ActorSystem),
                100,
                1d.seconds(),
                "",
                ""
            ));
            p.Tell(new TestCommand.Add("a", 3));
            ExpectMsg(new TestEvent.Appended("5", "A"));
            p.Tell(new TestCommand.Get());
            var state = ExpectMsg<TestState>();
            var words = state.Words.ToList();
            Assert.Equal(new [] { "A", "A", "A" }, words);

        }

        [Fact]
        public void notify_on_recover_complete()
        {
            var probe = CreateTestProbe();
            ActorSystem.ActorOf(ShardedEntityActorProps.Create<TestEntity, TestCommand, TestEvent, TestState>(
                "test",
                OptionInitializers.Some("2"),
                () => new TestEntity(ActorSystem, probe),
                100,
                1d.seconds(),
                "",
                ""
            ));
            probe.ExpectMsg<TestEntity.AfterRecovery>();
        }

        [Fact]
        public void actor_can_change_behavior()
        {
            {
                var p = ActorSystem.ActorOf(ShardedEntityActorProps.Create<TestEntity, TestCommand, TestEvent, TestState>(
                    "test",
                    OptionInitializers.Some("2"),
                    () => new TestEntity(ActorSystem),
                    100,
                    1d.seconds(),
                    "",
                    ""
                ));
                p.Tell(new TestCommand.Get());
                var state = ExpectMsg<TestState>();
                Assert.Equal(TestEntity.Mode.APPEND, state.Mode);
                p.Tell(new TestCommand.Add("a"));
                ExpectMsg(new TestEvent.Appended("2", "A"));
                p.Tell(new TestCommand.Add("b"));
                ExpectMsg(new TestEvent.Appended("2", "B"));
                p.Tell(new TestCommand.ChangeMode(TestEntity.Mode.PREPEND));
                ExpectMsg(new TestEvent.InPrependMode("2"));
                p.Tell(new TestCommand.Add("C"));
                ExpectMsg(new TestEvent.Prepended("2", "c"));
                p.Tell(new TestCommand.Get());
                var state2 = ExpectMsg<TestState>();
                var words = state2.Words.ToList();
                Assert.Equal(words, new[] {"c", "A", "B"});
            }

            {
                var p = ActorSystem.ActorOf(ShardedEntityActorProps.Create<TestEntity, TestCommand, TestEvent, TestState>(
                    "test",
                    OptionInitializers.Some("2"),
                    () => new TestEntity(ActorSystem),
                    100,
                    1d.seconds(),
                    "",
                    ""
                ));
                p.Tell(new TestCommand.Get());
                var state = ExpectMsg<TestState>();
                Assert.Equal(TestEntity.Mode.PREPEND, state.Mode);
                p.Tell(new TestCommand.Add("D"));
                ExpectMsg(new TestEvent.Prepended("2", "d"));
                p.Tell(new TestCommand.Get());
                var state2 = ExpectMsg<TestState>();
                var words = state2.Words.ToList();
                Assert.Equal(words, new[] { "d", "c", "A", "B" });
            }

            {
                var p = ActorSystem.ActorOf(ShardedEntityActorProps.Create<TestEntity, TestCommand, TestEvent, TestState>(
                    "test",
                    OptionInitializers.Some("2"),
                    () => new TestEntity(ActorSystem),
                    100,
                    1d.seconds(),
                    "",
                    ""
                ));
                p.Tell(new TestCommand.ChangeMode(TestEntity.Mode.APPEND));
                ExpectMsg(new TestEvent.InAppendMode("2"));
                p.Tell(new TestCommand.Add("e"));
                ExpectMsg(new TestEvent.Appended("2", "E"));
                p.Tell(new TestCommand.Get());
                var state2 = ExpectMsg<TestState>();
                var words = state2.Words.ToList();
                Assert.Equal(words, new[] { "d", "c", "A", "B", "E" });
            }

        }

        [Fact]
        public void actor_will_persist_events()
        {
            {
                var p = ActorSystem.ActorOf(ShardedEntityActorProps.Create<TestEntity, TestCommand, TestEvent, TestState>(
                    "test",
                    OptionInitializers.Some("1"),
                    () => new TestEntity(ActorSystem),
                    100,
                    5d.seconds(),
                    "",
                    ""
                ));
                p.Tell(new TestCommand.Get());
                var state = ExpectMsg<TestState>();
                Assert.Empty(state.Words);
                p.Tell(new TestCommand.Add("a"));
                ExpectMsg(new TestEvent.Appended("1", "A"));
                p.Tell(new TestCommand.Add("b"));
                ExpectMsg(new TestEvent.Appended("1", "B"));
                p.Tell(new TestCommand.Add("c"));
                ExpectMsg(new TestEvent.Appended("1", "C"));
                p.Tell(new TestCommand.Get());
                var state2 = ExpectMsg<TestState>();
                var words = state2.Words.ToList();
                Assert.Equal(words, new[] { "A", "B", "C" });
            }

            // Check actor restoration
            {
                var p = ActorSystem.ActorOf(ShardedEntityActorProps.Create<TestEntity, TestCommand, TestEvent, TestState>(
                    "test",
                    OptionInitializers.Some("1"),
                    () => new TestEntity(ActorSystem),
                    100,
                    1d.seconds(),
                    "",
                    ""
                ));
                p.Tell(new TestCommand.Get());
                var state = ExpectMsg<TestState>();
                var words = state.Words.ToList();
                Assert.Equal(words, new[] { "A", "B", "C" });
            }

        }

        [Fact]
        public void actor_will_passivate_after_idle()
        {
            var p = ActorSystem.ActorOf(Props.Create(typeof(PassivatingActor)));
            p.Tell(new TestCommand.Add("a"));
            ExpectMsg<TestEvent.Appended>();
            var entity = LastSender;
            Watch(entity);
            ExpectTerminated(entity);
        }
    }
}