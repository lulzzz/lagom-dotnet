using System;

namespace wyvern.utils
{
    /// <summary>
    /// Various Double extensions
    /// </summary>
    public static class DoubleExtensions
    {
        /// <summary>
        /// Creates a timespan in seconds from the given double
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static TimeSpan seconds(this double d)
        {
            return TimeSpan.FromSeconds(d);
        }

        /// <summary>
        /// Creates a timespan in minutes from the given double
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static TimeSpan minutes(this double d)
        {
            return TimeSpan.FromMinutes(d);
        }

        /// <summary>
        /// Creates a timespan in hours from the given double
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static TimeSpan hours(this double d)
        {
            return TimeSpan.FromHours(d);
        }

        /// <summary>
        /// Creates a timespan in hours from the given double
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static TimeSpan days(this double d)
        {
            return TimeSpan.FromDays(d);
        }
    }

}
