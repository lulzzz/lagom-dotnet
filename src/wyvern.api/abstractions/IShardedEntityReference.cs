using System.Threading.Tasks;

namespace wyvern.api.abstractions
{
    public interface IShardedEntityReference
    {
        Task<TR> Ask<TR>(IReplyType<TR> command);
    }
}
