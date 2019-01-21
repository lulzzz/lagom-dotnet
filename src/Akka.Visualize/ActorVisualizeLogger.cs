using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;

namespace Akka.Visualize
{
	public class ActorVisualizeLogger : TypedActor
	{
		public const string Name = "AkkaVisualizer";

		protected override void PreStart()
		{
			base.PreStart();
		}

		protected override void PreRestart(Exception reason, object message)
		{
			base.PreRestart(reason, message);
		}

		protected override void PostRestart(Exception reason)
		{
			base.PostRestart(reason);
		}

	}
}
