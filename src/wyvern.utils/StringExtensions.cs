using System;
using System.IO;
using System.Linq;
using System.Text;

namespace wyvern.utils
{
    public static class Preconditions
    {
        public static void IsNotNull<T>(this T thing, string errorMessage) where T : class
        {
            if (thing == null) throw new NullReferenceException(errorMessage);
        }
    }

    public static class StringExtensions
    {
        public static string ToShardId(this string str, int maxShards)
        {
            return (str.Sum(x => (int)x) % maxShards).ToString();
        }

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
                        sb.AppendLine(tmp);
                    else
                        sb.Append(line);
                }
            }
            return sb.ToString();
        }
    }

    public static class DoubleExtensions
    {
        public static TimeSpan seconds(this double d)
        {
            return TimeSpan.FromSeconds(d);
        }
    }

}
