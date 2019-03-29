using System;
using System.Threading.Tasks;
using Akka;
using Akka.Persistence.Query;
using wyvern.api.abstractions;

namespace wyvern.api.@internal.surfaces
{
    /// <summary>
    /// Offset DAO for local development, not to be used in production
    /// </summary>
    [Obsolete("Not for production use")]
    public class InMemoryOffsetDao : IOffsetDao
    {
        public Offset LoadedOffset { get; private set; } = Offset.NoOffset();

        public Task<Done> SaveOffset(Offset o)
        {
            LoadedOffset = o;
            return Task.FromResult(Done.Instance);
        }
    }
}
