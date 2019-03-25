using System.Threading.Tasks;
using wyvern.api.abstractions;

namespace wyvern.api.@internal.surfaces
{
    /// <summary>
    /// Offset Store for local development, not to be used in production
    /// </summary>
    public class InMemoryOffsetStore : IOffsetStore
    {
        public Task<IOffsetDao> Prepare(string processorId, string tag)
        {
            return Task.FromResult<IOffsetDao>(new InMemoryOffsetDao());
        }
    }
}
