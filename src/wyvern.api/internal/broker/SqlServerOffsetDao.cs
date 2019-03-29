using System.Threading.Tasks;
using Akka;
using Akka.Persistence.Query;
using wyvern.api.abstractions;

namespace wyvern.api.@internal.surfaces
{
    public class SqlServerOffsetDao : IOffsetDao
    {
        SqlServerOffsetStore Store { get; }

        string ReadSideId { get; }
        string Tag { get; }

        public Offset LoadedOffset { get; }

        public SqlServerOffsetDao(SqlServerOffsetStore store, string readSideId, string tag, Offset loadedOffset)
        {
            Store = store;
            ReadSideId = readSideId;
            Tag = tag;
            LoadedOffset = loadedOffset;
        }

        public Task<Done> SaveOffset(Offset o)
        {
            return Store.UpdateOffset(ReadSideId, Tag, o);
        }
    }

}
