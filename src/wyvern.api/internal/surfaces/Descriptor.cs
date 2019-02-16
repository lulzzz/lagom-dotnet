using wyvern.api.abstractions;

namespace wyvern.api.@internal.surfaces
{
    internal static class Descriptor
    {
        internal static IDescriptor Named(string name)
        {
            return new DescriptorImpl(name);
        }
    }
}