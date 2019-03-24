using wyvern.entity.@event.aggregate;

public abstract partial class HelloEvent
{

    /// <summary>
    /// Allocates the entity tag which delineates the domain activity within
    /// the event stream
    /// </summary>
    /// <returns></returns>
    public new static readonly AggregateEventTag Tag =
        AggregateEventTag.Of<HelloEvent>(
            "HelloEvent"
        );

    /// <summary>
    /// Aggregate tag reference
    /// </summary>
    public override IAggregateEventTagger AggregateTag => Tag;

    /// <summary>
    /// Provide an aggregate tag instance
    /// </summary>
    public class HelloEventTag
    {
        public static AggregateEventTag Instance { get; } = AggregateEventTag.Of<HelloEvent>();
    }
}
