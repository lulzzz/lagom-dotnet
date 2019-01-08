using System;
using System.Linq;

namespace wyvern.utils
{
    public static class Preconditions
    {
        public static void IsNotNull<T>(this T thing) where T : class
        {
            if (thing == null) throw new NullReferenceException("Value should not be null");
        }

        public static void IsNotNull<T>(this T thing, string errorMessage) where T : class
        {
            if (thing == null) throw new NullReferenceException(errorMessage);
        }

        public static void IsPositive(decimal initialBalance)
        {
            if (initialBalance <= 0) throw new InvalidOperationException("Value should be a positive number");
        }

        public static void IsPositive(decimal initialBalance, string errorMessage)
        {
            if (initialBalance <= 0) throw new InvalidOperationException(errorMessage);
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
