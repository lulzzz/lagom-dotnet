// using System;
// using System.Data.Common;
// using System.Data.SqlClient;
// using Microsoft.Extensions.Configuration;
// using Microsoft.Extensions.Logging;
// using wyvern.entity.@event.aggregate;

// public class SqlServerReadSideImpl : ISqlServerReadSide
// {
//     IConfiguration Config { get; }
//     ILogger Log { get; }

//     public SqlServerReadSideImpl(ILogger log, IConfiguration config)
//     {
//         Config = config;
//         Log = log;
//     }

//     public IReadSideHandlerBuilder<TE> Builder<TE>(string readSideId) where TE : AggregateEvent<TE>
//     {
//         var constr = Config.GetConnectionString(readSideId);
//         Func<SqlConnection> factory = () => new SqlConnection(constr);
//         return new ReadSideHandlerBuilder<TE>(readSideId);
//     }
// }



