using System.IO;
using System.Text;
using Newtonsoft.Json;
using wyvern.api.abstractions;

namespace wyvern.utils
{
    public class DefaultSerializer : ISerializer
    {
        public byte[] Serialize<T>(T obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            var b = Encoding.UTF8.GetBytes(json);
            return b;
        }
    }

}