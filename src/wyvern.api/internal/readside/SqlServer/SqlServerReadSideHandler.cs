using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Akka;
using Akka.Persistence.Query;
using Akka.Streams.Dsl;
using wyvern.entity.@event.aggregate;

public class SqlServerReadSideHandler<TE> : ReadSideHandler<TE>
     where TE : AggregateEvent<TE>
{
    //     @volatile
    //     private var offsetDao: SlickOffsetDao = _

    public string ReadSideId { get; }

    public Action<SqlConnection> GlobalPrepareCallback { get; }
    public Action<SqlConnection, AggregateEventTag> PrepareCallback { get; }
    public Dictionary<Type, Action<SqlConnection, TE, Offset>> EventHandlers { get; }

    public SqlServerReadSideHandler(
        Action<SqlConnection> globalPrepareCallback,
        Action<SqlConnection, AggregateEventTag> prepareCallback,
        Dictionary<Type, Action<SqlConnection, TE, Offset>> eventHandlers
    )
    {
        GlobalPrepareCallback = globalPrepareCallback;
        PrepareCallback = prepareCallback;
        EventHandlers = eventHandlers;

    }

    public override Task<Done> GlobalPrepare()
    {
        /*
            slick.ensureTablesCreated().flatMap { _ =>
                slick.db.run {
                    SimpleDBIO { ctx =>
                        globalPrepareCallback(ctx.connection)
                        Done.getInstance()
                    }
                }
            }.toJava
        */
        return base.GlobalPrepare();
    }

    public override Task<Offset> Prepare(AggregateEventTag tag)
    {

        //     override def prepare(tag: AggregateEventTag[Event]): CompletionStage[Offset] = {
        //       (for {
        //         _<- slick.db.run {
        //           SimpleDBIO { ctx =>
        //             prepareCallback(ctx.connection, tag)
        //           }
        //         }
        //         dao<- offsetStore.prepare(readSideId, tag.tag)
        //       } yield {
        //         offsetDao = dao
        //         OffsetAdapter.offsetToDslOffset(dao.loadedOffset)
        //       }).toJava
        //     }
        return Task.FromResult(Offset.NoOffset());
    }

    public override Flow<(TE, Offset), Done, NotUsed> Handle()
    {
        return Flow.FromFunction(new Func<(TE, Offset), Done>(tuple => Done.Instance));

        //     override def handle(): Flow[Pair[Event, Offset], Done, Any] = {

        //       akka.stream.scaladsl.Flow[Pair[Event, Offset]]
        //         .mapAsync(parallelism = 1)
        // {
        //     pair =>

        // val dbAction = eventHandlers.get(pair.first.getClass)
        // .map {
        //         handler =>
        //    // apply handler if found
        //    val castedHandler = handler.asInstanceOf[(Connection, Event, Offset) => Unit]
        //               SimpleDBIO { ctx => castedHandler(ctx.connection, pair.first, pair.second) }
        //     }
        //             .getOrElse {
        //         // fallback to empty action if no handler is found
        //         if (log.isDebugEnabled) log.debug("Unhandled event [{}]", pair.first.getClass.getName)
        //               DBIO.successful(())
        //             }
        //             .flatMap {
        //         _ =>
        // // whatever it happens we save the offset
        // offsetDao.updateOffsetQuery(OffsetAdapter.dslOffsetToOffset(pair.second))
        //             }
        //             .map(_ => Done)

        //           slick.db.run(dbAction.transactionally)

        //         }.asJava
        //     }
        //   }
        // }
    }

}
