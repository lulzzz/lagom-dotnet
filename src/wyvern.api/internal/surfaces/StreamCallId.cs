using wyvern.utils;

namespace wyvern.api.@internal.surfaces
{
    [Immutable]
    internal sealed class StreamCallId : CallId
    {
        /// <summary>
        /// Path pattern implementation
        /// </summary>
        public string PathPattern { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="method"></param>
        /// <param name="pathPattern"></param>
        internal StreamCallId(string pathPattern)
        {
            (PathPattern) =
            (pathPattern[0] == '/' ? pathPattern : $"/{pathPattern}");
        }

        public override bool Equals(object o)
        {
            if (this == o) return true;
            if (o.GetType() != GetType()) return false;
            var that = (StreamCallId)o;
            return PathPattern.Equals(that.PathPattern);
        }

        public override int GetHashCode()
        {
            return PathPattern.GetHashCode();
        }

        public override string ToString()
        {
            return "StreamCallId{" +
                   "pathPattern='" + PathPattern + '\'' +
                   '}';
        }
    }
}
