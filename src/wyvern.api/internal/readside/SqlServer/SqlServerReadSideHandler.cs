using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Akka;
using Akka.Persistence.Query;
using Akka.Streams.Dsl;
using Dapper;
using Microsoft.Extensions.Configuration;
using wyvern.entity.@event.aggregate;

namespace wyvern.api.@internal.readside.SqlServer
{
    public class SqlServerReadSideHandler<TE> : ReadSideHandler<TE>
        where TE : AggregateEvent<TE>
    {
        private static Func<string, Func<SqlConnection>> ReadSideConnectionFactoryInitializer { get; }
            = (constr) => () => new SqlConnection(constr);

        protected Func<SqlConnection> ReadSideConnectionFactory { get; }
        public string ReadSideId { get; }
        public Action<SqlConnection> GlobalPrepareCallback { get; }
        public Action<SqlConnection, AggregateEventTag> PrepareCallback { get; }
        public Dictionary<Type, Action<SqlConnection, TE, Offset>> EventHandlers { get; }

        public SqlServerReadSideHandler(
            IConfiguration config,
            string readSideId,
            Action<SqlConnection> globalPrepareCallback,
            Action<SqlConnection, AggregateEventTag> prepareCallback,
            Dictionary<Type, Action<SqlConnection, TE, Offset>> eventHandlers
        )
        {
            ReadSideId = readSideId;
            GlobalPrepareCallback = globalPrepareCallback;
            PrepareCallback = prepareCallback;
            EventHandlers = eventHandlers;

            var constr = config.GetConnectionString(ReadSideId);
            ReadSideConnectionFactory = ReadSideConnectionFactoryInitializer(constr);
        }

        public override Task<Done> GlobalPrepare()
        {
            using (var con = ReadSideConnectionFactory.Invoke())
            {
                con.Execute(@"
            if not exists (select * from sysobjects where name='read_side_offsets' and xtype='U')
                CREATE TABLE read_side_offsets (read_side_id VARCHAR(255), tag VARCHAR(255),sequence_offset bigint, time_uuid_offset char(36),PRIMARY KEY (read_side_id, tag))
            ");
                GlobalPrepareCallback(con);
            }
            return base.GlobalPrepare();
        }

        public override Task<Offset> Prepare(AggregateEventTag tag)
        {
            using (var con = ReadSideConnectionFactory.Invoke())
            {
                var offset = con.QueryFirstOrDefault<long>(@"
                select sequence_offset
                from read_side_offsets
                where read_side_id = @readSideId
                and tag = @tag
            ", new
                {
                    readSideId = ReadSideId,
                    tag = tag.Tag
                });
                return Task.FromResult(
                    Offset.Sequence(
                        offset
                    )
                );
            }
        }

        public virtual void DbActionExecutor((TE, Offset) pair, Action<SqlConnection, TE, Offset> action)
        {
            try
            {
                using (var con = ReadSideConnectionFactory.Invoke())
                {
                    action(con, pair.Item1, pair.Item2);
                }
            } catch (Exception ex)
            {
                // TODO: 
            }
        }

        public override Flow<KeyValuePair<TE, Offset>, Done, NotUsed> Handle()
        {
            return Flow.FromFunction(
                new Func<KeyValuePair<TE, Offset>, Done>(
                    pair =>
                    {
                        if (EventHandlers.TryGetValue(pair.Key.GetType(), out var dbAction))
                        {
                                DbActionExecutor((pair.Key, pair.Value), dbAction);
                        }
                        else
                        {
                            // TODO: log the unhandled event
                        }

                        using (var con = ReadSideConnectionFactory.Invoke())
                        {
                            con.Execute(@"
                                update read_side_offsets
                                set sequence_offset = @offset
                                where read_side_id = @readSideId
                                and tag = @tag",
                                new
                                {
                                    offset = (pair.Value as Sequence).Value,
                                    readSideId = ReadSideId,
                                    tag = (pair.Key.AggregateTag as AggregateEventTag).Tag
                                });
                        }

                        return Done.Instance;
                    }
                )
            );
        }

    }
}
