using System;

namespace wyvern.api.exceptions
{
    public abstract class StatusCodeException : Exception
    {
        public StatusCodeException(string message) : base(message) { }
    }

    public sealed class ConflictException : StatusCodeException
    {
        public ConflictException(string message) : base(message) { }
    }

    public sealed class NotFoundException : StatusCodeException
    {
        public NotFoundException(string message) : base(message) { }
    }

    public sealed class BadRequestException : StatusCodeException
    {
        public BadRequestException(string message) : base(message) { }
    }

    public sealed class UnauthorizedException : StatusCodeException
    {
        public UnauthorizedException(string message) : base(message) { }
    }
}