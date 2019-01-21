using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
