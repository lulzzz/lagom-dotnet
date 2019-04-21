// using System;
// using System.Collections.Generic;
// using System.Data.SqlClient;
// using Akka.Persistence.Query;
// using wyvern.entity.@event;
// using wyvern.entity.@event.aggregate;

// public interface IReadSideHandlerBuilder<TE> where TE : AbstractEvent
// {
//     IReadSideHandlerBuilder<TE> SetGlobalPrepare(Func<SqlConnection> connectionFactory);
//     IReadSideHandlerBuilder<TE> SetPrepare(Func<(SqlConnection, AggregateEventTag)> connectionFactory);
//     IReadSideHandlerBuilder<TE> SetEventHandler<TEx>(Func<(SqlConnection, KeyValuePair<TEx, Offset>)> handler)
//         where TEx : TE;
// }



