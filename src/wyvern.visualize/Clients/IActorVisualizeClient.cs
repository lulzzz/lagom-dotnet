using System;

namespace wyvern.visualize.Clients
{
    /// <summary>
    /// Interfce for clients wanting to interact with the visualizer
    /// </summary>
    public interface IActorVisualizeClient : IDisposable
    {

        void SetVisualizer(IActorVisualizer actorVisualizer);
    }
}
