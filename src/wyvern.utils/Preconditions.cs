using System;

namespace wyvern.utils
{
    /// <summary>
    /// Set of exception throwing assertions
    /// </summary>
    public static class Preconditions
    {
        /// <summary>
        /// Asserts that the given entity is not null or throws a null reference exception
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void IsNotNull<T>(this T thing, string errorMessage) where T : class
        {
            if (thing == null) throw new NullReferenceException(errorMessage);
        }
    }

}
