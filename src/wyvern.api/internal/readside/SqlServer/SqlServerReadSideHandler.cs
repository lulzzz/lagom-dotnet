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

public class SqlServerReadSideHandler<TE> : ReadSideHandler<TE>
     where TE : AggregateEvent<TE>
{
    DbConnectionFactory DbConnectionFactory { get; }
    public string ReadSideId { get; }
    public Action<SqlConnection> GlobalPrepareCallback { get; }
    public Action<SqlConnection, AggregateEventTag> PrepareCallback { get; }
    public Dictionary<Type, Action<SqlConnection, TE, Offset>> EventHandlers { get; }

    public SqlServerReadSideHandler(
        DbConnectionFactory dbConnectionFactory,
        string readSideId,
        Action<SqlConnection> globalPrepareCallback,
        Action<SqlConnection, AggregateEventTag> prepareCallback,
        Dictionary<Type, Action<SqlConnection, TE, Offset>> eventHandlers
    )
    {
        DbConnectionFactory = dbConnectionFactory;
        ReadSideId = readSideId;
        GlobalPrepareCallback = globalPrepareCallback;
        PrepareCallback = prepareCallback;
        EventHandlers = eventHandlers;
    }

    public override Task<Done> GlobalPrepare()
    {
        using (var con = DbConnectionFactory.Create())
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
        using (var con = DbConnectionFactory.Create())
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

    public override Flow<(TE, Offset), Done, NotUsed> Handle()
    {
        return Flow.FromFunction(
            new Func<(TE, Offset), Done>(
                pair =>
                   {
                       if (EventHandlers.TryGetValue(pair.Item1.GetType(), out var dbAction))
                       {
                           using (var con = DbConnectionFactory.Create())
                           {
                               dbAction(con, pair.Item1, pair.Item2);
                           }
                       }
                       else
                       {
                           // TODO: log the unhandled event
                       }

                       using (var con = DbConnectionFactory.Create())
                       {
                           con.Execute(@"
                                update read_side_offsets
                                set sequence_offset = @offset
                                where read_side_id = @readSideId
                                and tag = @tag",
                               new
                               {
                                   offset = (pair.Item2 as Sequence).Value,
                                   readSideId = ReadSideId,
                                   tag = (pair.Item1.AggregateTag as AggregateEventTag).Tag
                               });
                       }

                       return Done.Instance;
                   }
                )
            );
    }

}
