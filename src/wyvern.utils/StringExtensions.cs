using System.IO;
using System.Linq;
using System.Text;

namespace wyvern.utils
{

    /// <summary>
    /// Various string extensions
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Hashes a string into a shard id
        /// </summary>
        /// <param name="str"></param>
        /// <param name="maxShards"></param>
        /// <returns></returns>
        public static string ToShardId(this string str, int maxShards)
        {
            return (str.Sum(x => (int)x) % maxShards).ToString();
        }

        /// <summary>
        /// Trims all whitespace on each line of the string up until the given
        /// margin decorator.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="marginDecorator"></param>
        /// <returns></returns>
        public static string TrimMargin(this string str, string marginDecorator = "|")
        {
            StringBuilder sb = new StringBuilder();
            using (var sr = new StringReader(str))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    var tmp = line.TrimStart(' ');
                    if (tmp.Length > 0 && tmp.StartsWith(marginDecorator))
                        sb.AppendLine(tmp.TrimStart('|'));
                    else
                        sb.Append(line);
                }
            }
            return sb.ToString();
        }
    }

}
