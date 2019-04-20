namespace wyvern.api.exceptions
{
    /// <summary>
    /// Exception with 400 Bad Request
    /// </summary>
    public sealed class BadRequestException : StatusCodeException
    {
        public BadRequestException(string message) : base(message) { }
    }
}
