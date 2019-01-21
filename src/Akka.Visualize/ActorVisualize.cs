using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Visualize.Clients;
using Akka.Visualize.Interop;
using Akka.Visualize.Models;

namespace Akka.Visualize
{
    public class ActorVisualize : IExtension, IActorVisualizer
    {
        private readonly ActorSystem _system;
        private readonly VisualizeRegistry _registry = new VisualizeRegistry();
        private IActorRef _queryActor;

        public ActorVisualize(ActorSystem system)
        {
            _system = system;

            _queryActor = _system.ActorOf(Props.Create<QueryActor>());
        }

        public bool RegisterMonitor(IActorVisualizeClient client)
        {
            client.SetVisualizer(this);
            return _registry.AddMonitor(client);
        }

        public Task<QueryResult> List(string path)
        {
            if (String.IsNullOrEmpty(path))
            {

                return Task.FromResult(new QueryResult("", new List<NodeInfo>()
                {
                    new NodeInfo()
                    {
                        Path = _queryActor.Path.Root.ToString(),
                        Name = _queryActor.Path.Root.Address.System,
                        IsLocal = true,
                        IsTerminated = false,
                        Type = "akka"
                    }
                }));
            }

            if (!path.EndsWith("*"))
                path = path + "*";
            return _queryActor.Ask<QueryResult>(new Messages.Query(path));
        }

        public Task<NodeInfo> Send(string path, string messageType)
        {
            var message = GetMessage(messageType);
            if (message == null)
            {
                return Task.FromResult(new NodeInfo()
                {
                    Path = path
                });
            }

            _system.ActorSelection(path)
                .Tell(message);

            return Task.FromResult(new NodeInfo()
            {
                Path = path
            });
        }

        private object GetMessage(string messageType)
        {
            messageType = messageType.ToLower();
            switch (messageType)
            {
                case "kill":
                    return Kill.Instance;
                case "poisonpill":
                    return PoisonPill.Instance;
                default:
                    return null;
            }
        }
    }

    public interface IActorVisualizer
    {
        Task<QueryResult> List(string path);
        Task<NodeInfo> Send(string path, string messageType);
    }
}
