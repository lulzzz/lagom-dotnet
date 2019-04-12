using System.Collections.Generic;
using wyvern.api.abstractions;

namespace wyvern.utils
{
    public class DefaultExtractor : IMessagePropertyExtractor
    {
        public Dictionary<string, object> Extract<T>(T obj)
        {
            return new Dictionary<string, object>();
        }
    }

}