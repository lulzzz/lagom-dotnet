using System;
using System.Linq;

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
    }

    public static class DoubleExtensions
    {
        public static TimeSpan seconds(this double d)
        {
            return TimeSpan.FromSeconds(d);
        }
    }

}
