namespace wyvern.api.exceptions
{
    /// <summary>
    /// Exception with 404 Not Found
    /// </summary>
    public sealed class NotFoundException : StatusCodeException
    {
        public NotFoundException(string message) : base(message) { }
    }
}
