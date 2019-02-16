using System.Threading.Tasks;

namespace wyvern.api.abstractions
{
    public interface IShardedEntityReference
    {
        Task<object> Ask<TR>(IReplyType<TR> command);
    }
}
