namespace wyvern.entity.@event.aggregate
{
    public abstract class AggregateEvent<E> : AbstractEvent, IAggregateEvent
        where E : AggregateEvent<E>
    {
        public static readonly AggregateEventTag Tag = AggregateEventTag.Of<E>();
        public virtual IAggregateEventTagger AggregateTag => Tag;
    }
}
