using wyvern.entity.@event.aggregate;

public interface ISqlServerReadSide
{
    IReadSideHandlerBuilder<TE> Builder<TE>(string readSideId)
        where TE : AggregateEvent<TE>;
}
