using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace wyvern.api.tests
{
    

    public class ImmutableTests
    {
        [Fact]
        public void AssertImmutable_IDescriptor()
        {
            var types = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(x => x.Namespace != null && x.Namespace.StartsWith("wyvern"))
                .Where(x => x.IsClass)
                .Where(x => x.GetCustomAttributes(typeof(ImmutableAttribute), true).Length > 0);

            var errors = new List<string>();
            foreach (var type in types)
            {
                var subErrors = Immutable(type);
                if (subErrors.Length > 0)
                    errors.Add($"{type.Name} is not immutable: {string.Join(", ", subErrors)}");
            }
            
            Assert.True(errors.Count == 0, string.Join("\n", errors));
        }
        
        private static string Immutable(Type type)
        {
            if (type.IsPrimitive) return "";
            if (type == typeof(string)) return "";
            var fieldInfos = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var shallowFields = fieldInfos.Where(f => !f.IsInitOnly).ToArray();
            if (shallowFields.Any())
                return $"Shallow mutable fields on {type.Name}: \\n" + string.Join("\\n", shallowFields.Select(y => y.Name));
            
            var propertyInfos = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var mutableProperties = propertyInfos.Where(x => x.CanWrite).ToArray();
            if (mutableProperties.Any())
                return $"Mutable property on {type.Name}: \\n" + string.Join("\\n", mutableProperties.Select(y => y.Name));
            
            // TODO: Recursive mutability for field and property types

            var methodInfos = type.GetMethods((BindingFlags.Public | BindingFlags.NonPublic))
                .Where(x => x.DeclaringType != typeof(object))
                .Where(x => x.ReturnType != type) // TODO: derived return types, inheritance?
                .Where(x => !x.GetCustomAttributes(typeof(NoSideEffectsAttribute), true).Any());
            var areMethodsUntrusted = methodInfos
                .Select(x => x.Name)
                .ToArray();
            if (areMethodsUntrusted.Any())
                return $"Untrusted methods on {type.Name}: " + String.Join(", ", areMethodsUntrusted);
            
            return "";            

        }
    }
}