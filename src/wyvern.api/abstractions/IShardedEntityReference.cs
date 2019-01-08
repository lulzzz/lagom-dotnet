using System.Threading.Tasks;

namespace wyvern.api
{
    public interface IShardedEntityReference
    {
        Task<TR> Ask<TR>(IReplyType<TR> command);
    }
}