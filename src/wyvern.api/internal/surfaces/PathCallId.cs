namespace wyvern.api.@internal.surfaces
{
    /// <inheritdoc />
    /// <summary>
    ///     Path call identifier
    /// </summary>
    [Immutable]
    internal sealed class PathCallId : CallId
    {
        public string PathPattern { get; }

        internal PathCallId(string pathPattern)
        {
            PathPattern = pathPattern;
        }
        
        public override string ToString()
        {
            return PathPattern;
        }
        
        // TODO: Hash code
    }
}