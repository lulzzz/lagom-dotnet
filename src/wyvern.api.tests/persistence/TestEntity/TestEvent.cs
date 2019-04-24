using wyvern.entity.@event;

namespace wyvern.api.tests
{
    public abstract class TestEvent : AbstractEvent
    {
        public abstract class ChangedMode : TestEvent
        {
            private TestEntity.Mode Mode { get; }

            public string EntityId { get; }

            protected ChangedMode(TestEntity.Mode mode, string entityId)
            {
                Mode = mode;
                EntityId = entityId;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is ChangedMode item))
                {
                    return false;
                }

                return item.Mode == Mode && item.EntityId == EntityId;
            }

            public override int GetHashCode()
            {
                return Mode.GetHashCode() ^ EntityId.GetHashCode();
            }
        }

        public class InPrependMode : ChangedMode
        {
            public InPrependMode(string entityId): base(TestEntity.Mode.PREPEND, entityId) { }
            
        }

        public class InAppendMode : ChangedMode
        {
            public InAppendMode(string entityId) : base(TestEntity.Mode.APPEND, entityId) { }
        }

        public abstract class Added : TestEvent
        {
            private TestEntity.Mode Mode { get; } 

            public string EntityId { get; }
            public string Text { get; }

            protected Added(TestEntity.Mode mode, string entityId, string text)
            {
                EntityId = entityId;
                Text = text;
                Mode = mode;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is Added item))
                {
                    return false;
                }

                return item.Mode == Mode && item.EntityId == EntityId && item.Text == Text;
            }

            public override int GetHashCode()
            {
                return Mode.GetHashCode() ^ EntityId.GetHashCode() ^ Text.GetHashCode();
            }
        }

        public sealed class Appended : Added
        {
            public Appended(string entityId, string text) : base(TestEntity.Mode.APPEND, entityId, text)
            {
            }
        }

        public sealed class Prepended : Added
        {
            public Prepended(string entityId, string text) : base(TestEntity.Mode.PREPEND, entityId, text)
            {

            }
        }
    }
}