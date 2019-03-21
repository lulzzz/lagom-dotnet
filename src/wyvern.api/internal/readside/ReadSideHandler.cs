using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka;
using Akka.Persistence.Query;
using Akka.Streams.Dsl;
using wyvern.entity.@event.aggregate;

namespace wyvern.api.@internal.readside
{
    public class ReadSideHandler<TE> where TE : AggregateEvent<TE>
    {
        public virtual Task<Done> GlobalPrepare()
        {
            return Task.FromResult(Done.Instance);
        }

        public virtual Task<Offset> Prepare(AggregateEventTag tag)
        {
            return Task.FromResult(Offset.NoOffset());
        }

        public virtual Flow<KeyValuePair<TE, Offset>, Done, NotUsed> Handle()
        {
            throw new NotImplementedException();
        }
    }
}
