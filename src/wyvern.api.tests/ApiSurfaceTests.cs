using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Xunit;

namespace wyvern.api.tests
{
    public class ApiSurfaceTests
    {
        [Fact]
        public void classes_in_api_namespace_cannot_be_instantiated()
        {
            var types = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(x => x.Namespace != null 
                            && x.Namespace.StartsWith("wyvern.api", true, CultureInfo.InvariantCulture) 
                            && !x.Namespace.EndsWith("tests", true, CultureInfo.InvariantCulture)
                            )
                .Where(x => x.IsClass || x.IsInterface)
                .Where(x => x.IsPublic)
                .Where(x => x.GetCustomAttributes(typeof(TraitAttribute), true).Length == 0);

            var errors = new List<string>();
            foreach (var type in types)
            {
                /*
                 * API Objects are of three types:
                 *
                 * 0. Interfaces
                 */
                if (type.IsInterface) continue;
                /* 1. Classes provided for extension in abstract form
                 */
                var @class = type;
                if (@class.IsAbstract) continue;
                /*
                 * 2. Classes handled outside of the framework should not be instantiated from
                 * outside the framework.
                 */
                if (@class.GetConstructors().Any(y => y.IsPublic))
                    errors.Add($"{@class.Name} it not abstract or has public constructors");
            }

            Assert.True(errors.Count == 0, string.Join("\n", errors));
        }
    }
}