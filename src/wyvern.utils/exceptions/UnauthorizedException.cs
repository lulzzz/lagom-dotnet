namespace wyvern.api.exceptions
{
    /// <summary>
    /// Exception with 401 Unauthorized
    /// </summary>
    public sealed class UnauthorizedException : StatusCodeException
    {
        public UnauthorizedException(string message) : base(message) { }
    }
}
