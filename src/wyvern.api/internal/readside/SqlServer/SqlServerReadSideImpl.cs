using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using wyvern.entity.@event.aggregate;

public class SqlServerReadSideImpl : ISqlServerReadSide
{
    IConfiguration Config { get; }
    ILogger Log { get; }

    public SqlServerReadSideImpl(ILogger log, IConfiguration config)
    {
        Config = config;
        Log = log;
    }

    public IReadSideHandlerBuilder<TE> Builder<TE>(string readSideId) where TE : AggregateEvent<TE>
    {
        var factory = new DbConnectionFactory(readSideId, Config);
        return new ReadSideHandlerBuilder<TE>(factory, readSideId);
    }
}



