namespace wyvern.entity.@event.aggregate
{
    public abstract class AggregateEvent<E> : AbstractEvent, IAggregateEvent
        where E : AggregateEvent<E>
    {
        public abstract IAggregateEventTagger AggregateTag { get; }
    }
}
