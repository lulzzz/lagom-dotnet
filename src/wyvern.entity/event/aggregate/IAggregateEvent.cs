namespace wyvern.entity.@event.aggregate
{
    /// <summary>
    ///     Aggregate event
    /// </summary>
    /// <typeparam name="E"></typeparam>
    public interface IAggregateEvent : IEvent
    {
        /// <summary>
        ///     Aggregate tag
        /// </summary>
        /// <value></value>
        IAggregateEventTagger AggregateTag { get; }
    }
}
