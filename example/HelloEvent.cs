using wyvern.entity.@event;
using wyvern.entity.@event.aggregate;

public abstract partial class HelloEvent : AggregateEvent<HelloEvent>
{
    public class GreetingUpdatedEvent : HelloEvent
    {
        public string Message { get; }
        public GreetingUpdatedEvent(string message) => Message = message;
    }
}
