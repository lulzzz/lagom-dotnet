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

    /// <summary>
    /// Exception with 409 Conflict
    /// /// </summary>
    public sealed class ConflictException : StatusCodeException
    {
        public ConflictException(string message) : base(message) { }
    }

    /// <summary>
    /// Exception with 404 Not Found
    /// </summary>
    public sealed class NotFoundException : StatusCodeException
    {
        public NotFoundException(string message) : base(message) { }
    }

    /// <summary>
    /// Exception with 400 Bad Request
    /// </summary>
    public sealed class BadRequestException : StatusCodeException
    {
        public BadRequestException(string message) : base(message) { }
    }

    /// <summary>
    /// Exception with 401 Unauthorized
    /// </summary>
    public sealed class UnauthorizedException : StatusCodeException
    {
        public UnauthorizedException(string message) : base(message) { }
    }
}
