using System;

namespace wyvern.utils
{
    [AttributeUsage(AttributeTargets.Class|AttributeTargets.Interface|AttributeTargets.Property)]
    public class ImmutableAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ImmutablePropertyAttribute : Attribute
    {
        
    }

    [AttributeUsage(AttributeTargets.Method)]
    public  class NoSideEffectsAttribute : Attribute
    {
        
    }
}