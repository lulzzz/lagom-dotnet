using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Akka.Persistence.Query;
using wyvern.entity.@event.aggregate;

public class ReadSideHandlerBuilder<TE> : IReadSideHandlerBuilder<TE>
    where TE : AggregateEvent<TE>
{

    string ReadSideId { get; }

    Action<SqlConnection> globalPrepare = x => { };
    Action<SqlConnection, AggregateEventTag> prepare = (x, y) => { };
    private Dictionary<Type, Action<SqlConnection, TE, Offset>> EventHandlers = new Dictionary<Type, Action<SqlConnection, TE, Offset>>();

    public ReadSideHandlerBuilder(string readSideId)
    {
        ReadSideId = readSideId;
    }

    public IReadSideHandlerBuilder<TE> SetEventHandler<TE2>(Action<SqlConnection, EventStreamElement<TE2>> handler) where TE2 : class
    {
        // TODO: ????
        // EventHandlers.Add(typeof(TE2), (x, y, z) => handler(x, y));

        // override def setEventHandler[E <: Event](eventClass: Class[E], handler: ConnectionBiConsumer[E]): ReadSideHandlerBuilder[Event] = {
        //       eventHandlers += (eventClass -> ((c: Connection, e: E, o: Offset) => handler.accept(c, e)))
        //       this
        //     }

        // override def setEventHandler[E <: Event](eventClass: Class[E], handler: ConnectionTriConsumer[E, Offset]): ReadSideHandlerBuilder[Event] = {
        //       eventHandlers += (eventClass -> handler.accept _)
        //       this
        //     }

        return this;
    }

    public IReadSideHandlerBuilder<TE> SetGlobalPrepare(Action<SqlConnection> callback)
    {
        globalPrepare = callback;
        return this;
    }

    public IReadSideHandlerBuilder<TE> SetPrepare(Action<SqlConnection, AggregateEventTag> callback)
    {
        prepare = callback;
        return this;
    }

    public ReadSideHandler<TE> Build()
    {
        return new SqlServerReadSideHandler<TE>(ReadSideId, globalPrepare, prepare, EventHandlers);
    }
}

