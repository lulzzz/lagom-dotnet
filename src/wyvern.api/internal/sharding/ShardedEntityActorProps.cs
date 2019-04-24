using System;
using Akka.Actor;
using Akka.Streams.Util;
using wyvern.api.ioc;
using wyvern.entity.command;
using wyvern.entity.@event;
using wyvern.entity.state;

namespace wyvern.api.@internal.sharding
{
    internal static class ShardedEntityActorProps
    {
        /// <summary>
        /// Props initializer
        /// </summary>
        /// <param name="persistenceIdPrefix"></param>
        /// <param name="entityId"></param>
        /// <param name="entityFactory"></param>
        /// <param name="snapshotAfter"></param>
        /// <param name="passivateAfterIdleTimeout"></param>
        /// <param name="snapshotPluginId"></param>
        /// <param name="journalPluginId"></param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TC"></typeparam>
        /// <typeparam name="TE"></typeparam>
        /// <typeparam name="TS"></typeparam>
        /// <returns></returns>
        internal static Props Create<T, TC, TE, TS>(
            string persistenceIdPrefix,
            Option<string> entityId,
            Func<T> entityFactory,
            int snapshotAfter,
            TimeSpan passivateAfterIdleTimeout,
            string snapshotPluginId,
            string journalPluginId)
            where T : ShardedEntity<TC, TE, TS>
            where TC : AbstractCommand
            where TE : AbstractEvent
            where TS : AbstractState
        {
            return Props.Create(() =>
                new ShardedEntityActor<T, TC, TE, TS>(
                    persistenceIdPrefix,
                    entityId,
                    entityFactory(),
                    snapshotAfter,
                    passivateAfterIdleTimeout,
                    snapshotPluginId,
                    journalPluginId
                )
            );
        }
    }
}
