using Akka.Persistence.Query;
using Akka.Streams.Util;
using wyvern.api.abstractions;

namespace wyvern.api.@internal.readside
{
    internal static class EventStreamElement
    {
        public static Option<(string, TE, Offset)> Unapply<TE>(EventStreamElement<TE> element) where TE : EventStreamElement<TE> =>
            new Option<(string, TE, Offset)>(
                (element.EntityId, element.Event, element.Offset)
            );
    }

    internal class EventStreamElement<TE> : IEventStreamElement<TE>
        where TE : class
    {
        public string EntityId { get; }
        public TE Event { get; }
        public Offset Offset { get; }

        public EventStreamElement(string entityId, TE @event, Offset offset)
        {
            EntityId = entityId;
            Event = @event;
            Offset = offset;
        }

        // TODO:

        //   override def equals(other: Any): Boolean = other match {
        //     case that: EventStreamElement[_] =>
        //       entityId == that.entityId &&
        //         event == that.event &&
        //         offset == that.offset
        //     case _ => false
        //   }

        //   override def hashCode(): Int = {
        //         val state = Seq[Any](entityId, event, offset)
        //     state.map(_.hashCode()).foldLeft(0)((a, b) => 31 * a + b)
        //   }

        //   override def toString = s"EventStreamElement($entityId, $event, $offset)"


    }
}