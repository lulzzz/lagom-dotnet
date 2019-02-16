using wyvern.utils;

namespace wyvern.api
{
    /// <summary>
    /// HTTP Method types
    /// </summary>
    [Immutable]
    public class Method
    {
        public static readonly Method GET = new Method("GET");
        public static readonly Method POST = new Method("POST");
        public static readonly Method PUT = new Method("PUT");
        public static readonly Method DELETE = new Method("DELETE");
        public static readonly Method HEAD = new Method("HEAD");
        public static readonly Method OPTIONS = new Method("OPTIONS");
        public static readonly Method PATCH = new Method("PATCH");

        private string Name { get; }

        private Method(string name)
        {
            Name = name;
        }

        public override bool Equals(object o)
        {
            if (this == o) return true;
            if (o == null || GetType() != o.GetType()) return false;

            var method = (Method) o;

            return Name.Equals(method.Name);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}