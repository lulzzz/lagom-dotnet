using Microsoft.Extensions.Logging;
using wyvern.entity.@event.aggregate;

public class SqlServerReadSideImpl : ISqlServerReadSide
{
    ILogger Log { get; }

    public SqlServerReadSideImpl(ILogger log)
    {
        Log = log;
        /*
        (slick: SlickProvider, offsetStore: JavadslJdbcOffsetStore)
        (implicit val ec: ExecutionContext) extends JdbcReadSide
         */
    }

    public IReadSideHandlerBuilder<TE> Builder<TE>(string readSideId) where TE : AggregateEvent<TE>
    {
        return new ReadSideHandlerBuilder<TE>(readSideId);
    }
}



