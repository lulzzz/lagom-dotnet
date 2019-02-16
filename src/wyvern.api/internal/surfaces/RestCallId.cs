using wyvern.utils;

namespace wyvern.api.@internal.surfaces
{
    /// <inheritdoc />
    /// <summary>
    /// Call identifier over REST
    /// </summary>
    [Immutable]
    internal sealed class RestCallId : CallId
    {
        /// <summary>
        /// Method implementation
        /// </summary>
        public Method Method { get; }

        /// <summary>
        /// Path pattern implementation
        /// </summary>
        public string PathPattern { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="method"></param>
        /// <param name="pathPattern"></param>
        internal RestCallId(Method method, string pathPattern)
        {
            (Method, PathPattern) = (method, pathPattern[0] == '/' ? pathPattern : $"/{pathPattern}");
        }

        public override bool Equals(object o)
        {
            if (this == o) return true;
            if (o.GetType() != GetType()) return false;
            var that = (RestCallId)o;
            return Method.Equals(that.Method) && PathPattern.Equals(that.PathPattern);
        }

        public override int GetHashCode()
        {
            var result = Method.GetHashCode();
            result = 31 * result + PathPattern.GetHashCode();
            return result;
        }

        public override string ToString()
        {
            return "RestCallId{" +
                   "method=" + Method +
                   ", pathPattern='" + PathPattern + '\'' +
                   '}';
        }
    }
}
