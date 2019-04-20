using System.Collections.Generic;

namespace wyvern.visualize.Models
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
