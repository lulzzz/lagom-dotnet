
using System.Threading.Tasks;
using Akka;
using Akka.Persistence.Query;
using Akka.Streams.Dsl;
using wyvern.entity.@event.aggregate;

public abstract class ReadSideProcessor<TE>
    where TE : AggregateEvent<TE>
{
    // TODO: check aggregateeventtag isn't typed
    public abstract AggregateEventTag[] AggregateTags { get; }
    public string ReadSideName => GetType().Name;
    public abstract ReadSideHandler<TE> BuildHandler();
}

public static class ReadSideProcessor
{
    public abstract class ReadSideHandler<TE> where TE : AggregateEvent<TE>
    {
        public Task<Done> GlobalPrepare() => Task.FromResult(Done.Instance);
        // TODO: check aggregateeventtag isn't typed
        public Task<Offset> Prepare(AggregateEventTag tag) => Task.FromResult<Offset>(NoOffset.Instance);
        public abstract Flow<EventStreamElement<TE>, Done, NotUsed> Handle();
    }
}
