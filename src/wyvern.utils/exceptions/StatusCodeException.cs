using System;

namespace wyvern.api.exceptions
{
    /// <summary>
    /// Exception with HTTP status code
    /// </summary>
    public abstract class StatusCodeException : Exception
    {
        public StatusCodeException(string message) : base(message) { }
    }
}
