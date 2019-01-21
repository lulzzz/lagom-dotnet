using System.Threading.Tasks;
using Akka.Visualize;
using Akka.Visualize.Clients;
using Akka.Visualize.Models;

namespace wyvern.api
{
    public class WebApiVisualizer : IActorVisualizeClient
    {
        private IActorVisualizer _actorVisualizer;

        public static WebApiVisualizer Root { get; internal set; }

        public WebApiVisualizer()
        {
            Root = this;
        }

        public void SetVisualizer(IActorVisualizer actorVisualizer)
        {
            _actorVisualizer = actorVisualizer;
        }

        public Task<QueryResult> List(string path)
        {
            return _actorVisualizer.List(path);
        }

        public Task<NodeInfo> Send(string path, string messageType)
        {
            return _actorVisualizer.Send(path, messageType);
        }

        public void Dispose()
        {

        }
    }
}