using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Akka.Visualize.Models
{
	public class QueryResult
	{
		public QueryResult(string path, List<NodeInfo> children)
		{
			Path = path;
			Children = children;
		}

		public string Path { get; }
		public List<NodeInfo> Children { get; }
	}
}
