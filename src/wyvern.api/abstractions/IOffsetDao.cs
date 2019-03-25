using System.Threading.Tasks;
using Akka;
using Akka.Persistence.Query;

namespace wyvern.api.abstractions
{
    public interface IOffsetDao
    {
        Offset LoadedOffset { get; }
        Task<Done> SaveOffset(Offset o);
    }
}
