using System;

namespace wyvern.api.exceptions
{
    /// <summary>
    ///     Exception denoting an invalid command
    /// </summary>
    public class InvalidCommandException : Exception
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        internal InvalidCommandException(string message) : base(message)
        {
        }
    }
}