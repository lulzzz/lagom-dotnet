using System;
using wyvern.entity.@event.aggregate;

namespace wyvern.api.@internal.readside
{
    public abstract class ReadSide
    {
        public abstract void Register<TE>(
            Func<ReadSideProcessor<TE>> processorFactory
        ) where TE : AggregateEvent<TE>;
    }
}
