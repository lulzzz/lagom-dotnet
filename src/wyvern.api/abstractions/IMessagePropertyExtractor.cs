using System.Collections.Generic;

namespace wyvern.api.abstractions
{
    public interface IMessagePropertyExtractor
    {
        Dictionary<string, object> Extract<T>(T obj);
    }
}