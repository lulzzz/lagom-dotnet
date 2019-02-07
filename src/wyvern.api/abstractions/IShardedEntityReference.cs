using System.Threading.Tasks;

namespace wyvern.api
{
    public interface IShardedEntityReference
    {
        Task<object> Ask<TR>(IReplyType<TR> command);
    }
}
