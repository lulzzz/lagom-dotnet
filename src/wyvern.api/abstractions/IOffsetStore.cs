using System.Threading.Tasks;

namespace wyvern.api.abstractions
{
    public interface IOffsetStore
    {
        Task<IOffsetDao> Prepare(string processorId, string tag);
    }
}
