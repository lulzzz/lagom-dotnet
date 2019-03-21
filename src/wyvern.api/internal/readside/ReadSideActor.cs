using System;
using System.Collections.Generic;
using Akka;
using Akka.Actor;
using Akka.Persistence.Query;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.Streams.Util;
using wyvern.entity.@event.aggregate;
using wyvern.utils;

namespace wyvern.api.@internal.readside
{
    internal static class ReadSideActor
    {
        public static Props Props<TE>(
            ReadSideConfig config,
            ClusterStartupTask globalPrepareTask,
            Func<AggregateEventTag, Offset, Source<KeyValuePair<TE, Offset>, NotUsed>> eventStreamFactory,
            Func<ReadSideProcessor<TE>> processor
        ) where TE : AggregateEvent<TE>
        {
            return Akka.Actor.Props.Create(
                () => new ReadSideActor<TE>(
                    config,
                    globalPrepareTask,
                    eventStreamFactory,
                    processor
                )
            );
        }

        public class Start
        {
            public string TagName { get; }

            public Start(string tagName)
            {
                TagName = tagName;
            }
        }

    }

    public class ReadSideActor<TE> : ReceiveActor
        where TE : AggregateEvent<TE>
    {
        ReadSideConfig Config { get; }
        ClusterStartupTask GlobalPrepareTask { get; }
        Func<AggregateEventTag, Offset, Source<KeyValuePair<TE, Offset>, NotUsed>> EventStreamFactory { get; }
        Func<ReadSideProcessor<TE>> Processor { get; }

        internal Option<IKillSwitch> Shutdown { get; set; } = Option<IKillSwitch>.None;

        public ReadSideActor(
            ReadSideConfig config,
            ClusterStartupTask globalPrepareTask,
            Func<AggregateEventTag, Offset, Source<KeyValuePair<TE, Offset>, NotUsed>> eventStreamFactory,
            Func<ReadSideProcessor<TE>> processor)
        {
            Config = config;
            GlobalPrepareTask = globalPrepareTask;
            EventStreamFactory = eventStreamFactory;
            Processor = processor;

            Receive<ClusterDistributionExtensionProvider.EnsureActive>(x =>
            {
                var tagName = x.EntityId;
                var timeout = config.GlobalPrepareTimeout;
                // TODO: this was map, check for threading issues
                globalPrepareTask
                    .AskExecute(timeout)
                    .ContinueWith((y) => new ReadSideActor.Start(tagName))
                    .PipeTo(Self);
                Become(Start(tagName));
            });
        }

        public Action Start(string tagName) => () =>
        {
            var tag = new AggregateEventTag(typeof(TE), tagName);
            Receive<Done>(x => throw new InvalidOperationException("Stream terminated when it shouldn't"));
            Receive<ClusterDistributionExtensionProvider.EnsureActive>(x => { });
            Receive<Status.Failure>(failure => throw failure.Cause);
            Receive<ReadSideActor.Start>((msg) =>
            {
                var backoffSource = RestartSource.WithBackoff(
                    () =>
                    {
                        // TODO: SQL Server connection string
                        ReadSideHandler<TE> handler = Processor().BuildHandler();
                        var offsetTask = handler.Prepare(tag);
                        return Source.FromTask(offsetTask)
                            .InitialTimeout(Config.OffsetTimeout)
                            .ConcatMany(offset =>
                                {
                                    var eventStreamSource = EventStreamFactory(tag, offset);
                                    var userlandFlow = handler.Handle();
                                    return eventStreamSource.Via(userlandFlow);
                                }
                            );
                    },
                    Config.MinBackoff,
                    Config.MaxBackoff,
                    Config.RandomBackoffFactor
                );



                (var killSwitch, var streamDone) = backoffSource.ViaMaterialized(KillSwitches.Single<Done>(), Keep.Right)
                    .ToMaterialized(Sink.Ignore<Done>(), Keep.Both)
                    // TODO: Materializer static???
                    .Run(Context.Materializer());

                Shutdown = new Option<IKillSwitch>(killSwitch);
                streamDone.PipeTo(Self);

            });
        };

        protected override void PostStop() => Shutdown.ForEach(x => x.Shutdown());
    }
}
