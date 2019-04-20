using System.IO;
using System.Text;
using Newtonsoft.Json;
using wyvern.api.abstractions;

namespace wyvern.utils
{
    /// <summary>
    /// Default serializer to be used in serializing the body of a topic message
    /// </summary>
    public class DefaultSerializer : ISerializer
    {
        /// <summary>
        /// Serializer the given object into UTF-8 encoded JSON formatted string
        /// </summary>
        /// <param name="obj"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public byte[] Serialize<T>(T obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            var b = Encoding.UTF8.GetBytes(json);
            return b;
        }
    }

}