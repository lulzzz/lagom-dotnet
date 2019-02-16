using System;

namespace Akka.Visualize.Clients
{
	/// <summary>
	/// Interfce for clients wanting to interact with the visualizer
	/// </summary>
	public interface IActorVisualizeClient : IDisposable
	{

		void SetVisualizer(IActorVisualizer actorVisualizer);
	}
}
