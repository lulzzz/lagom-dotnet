namespace wyvern.api.exceptions
{
    /// <summary>
    /// Exception with 409 Conflict
    /// /// </summary>
    public sealed class ConflictException : StatusCodeException
    {
        public ConflictException(string message) : base(message) { }
    }
}
