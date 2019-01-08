namespace wyvern.entity.@event.aggregate
{
    public interface IAggregateEventTag : IAggregateEventTagger
    {
        string Tag { get; }
    }
}
