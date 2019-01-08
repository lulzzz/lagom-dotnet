using System;

using wyvern.entity.@event.aggregate;

public abstract class ReadSide
{
    public abstract void Register<TE>(
        Func<ReadSideProcessor<TE>> processorFactory
    ) where TE : AggregateEvent<TE>;
}
