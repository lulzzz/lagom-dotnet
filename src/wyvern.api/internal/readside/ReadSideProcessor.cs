using System.Threading.Tasks;
using Akka;
using Akka.Configuration;
using Akka.Persistence.Query;
using Akka.Streams.Dsl;
using Microsoft.Extensions.Configuration;
using wyvern.api.abstractions;
using wyvern.entity.@event.aggregate;

namespace wyvern.api.@internal.readside
{
    public abstract class ReadSideProcessor<TE>
        where TE : AggregateEvent<TE>
    {
        public IConfiguration Config { get; internal set; }
        public Config Config2 { get; internal set; }
        public abstract AggregateEventTag[] AggregateTags { get; }
        public string ReadSideName => GetType().Name;
        public abstract ReadSideHandler<TE> BuildHandler();
    }

    public static class ReadSideProcessor
    {
        public abstract class ReadSideHandler<TE> where TE : AggregateEvent<TE>
        {
            public Task<Done> GlobalPrepare() => Task.FromResult(Done.Instance);
            public Task<Offset> Prepare(AggregateEventTag tag) => Task.FromResult<Offset>(NoOffset.Instance);
            public abstract Flow<IEventStreamElement<TE>, Done, NotUsed> Handle();
        }
    }
}