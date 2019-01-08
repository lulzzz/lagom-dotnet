using System;
using System.Data.SqlClient;
using wyvern.entity.@event.aggregate;

public interface IReadSideHandlerBuilder<TE>
    where TE : AggregateEvent<TE>
{

    IReadSideHandlerBuilder<TE> SetGlobalPrepare(Action<SqlConnection> callback);
    IReadSideHandlerBuilder<TE> SetPrepare(Action<SqlConnection, AggregateEventTag> callback);
    IReadSideHandlerBuilder<TE> SetEventHandler<TE2>(Action<SqlConnection, EventStreamElement<TE2>> handler)
        where TE2 : class; // TODO: what was ClassTag??
    ReadSideHandler<TE> Build();
}
