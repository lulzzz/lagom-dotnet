namespace wyvern.entity.@event.aggregate
{
    public interface IAggregateEventShards
    {
        AggregateEventTag ForEntityId(string entityId);
    }
}
