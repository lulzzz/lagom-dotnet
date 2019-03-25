using System;
using Akka.Streams.Util;

namespace wyvern.utils
{
    /// <summary>
    /// Extensions for Akka.Streams.Util.Option
    /// </summary>
    public static class OptionExtensions
    {
        public static bool ForAll<T>(this Option<T> o, Func<T, bool> f)
        {
            return !o.HasValue || f(o.Value);
        }

        public static void ForEach<T>(this Option<T> options, Action<T> func) { if (options.HasValue) func(options.Value); }

        /// <summary>
        /// Choose either the option value (if it has one) or the provided alternate
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T OrElse<T>(this Option<T> o, T e)
        {
            return o.HasValue ? o.Value : e;
        }

        /// <summary>
        /// Map the input type to the output type via the given delegate
        /// </summary>
        /// <param name="o"></param>
        /// <param name="f"></param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="E"></typeparam>
        /// <returns></returns>
        public static Option<E> Map<T, E>(
            this Option<T> o,
            Func<T, E> f)
        {
            return o.HasValue ? new Option<E>(f(o.Value)) : Option<E>.None;
        }
    }

    public class OptionInitializers
    {
        /// <summary>
        /// Create 'some' instance of T
        /// </summary>
        /// <param name="obj"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Option<T> Some<T>(T obj)
        {
            return new Option<T>(obj);
        }
    }
}
