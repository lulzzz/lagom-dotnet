using System;
using wyvern.api;
using wyvern.api.abstractions;
using wyvern.api.@internal.surfaces;
using Xunit;

namespace wyvern.tests
{
    public class DescriptorTests
    {
        private (IDescriptor, IDescriptor) Harness()
        {
            var guid = Guid.NewGuid().ToString();
            Func<string, string> function = x => x;

            var desc_1 = Descriptor.Named(guid)
                .WithCalls(
                    new Call<string, string>(
                        new RestCallId(Method.GET, "/api/test/{id}"),
                        function.Method
                    )
                );
            var hash_1 = desc_1.GetHashCode();

            var desc_2 = desc_1.WithCalls(
                new Call<string, string>(
                    new RestCallId(Method.GET, "/api/test/{id}"),
                    function.Method
                )
            );
            var hash_2 = desc_2.GetHashCode();

            return (desc_1, desc_2);
        }

        [Fact]
        public void withcalls_adds_calls_immutably()
        {
            var (d1, d2) = Harness();

            // Different objects on return
            Assert.NotSame(d1, d2);
            Assert.NotEqual(d1.GetHashCode(), d2.GetHashCode());
            Assert.NotSame(d1.Calls, d2.Calls);

            // Calls are retained
            foreach (var call1 in d1.Calls) Assert.True(d2.Calls.Contains(call1));

            // Additional calls added
            Assert.True(d2.Calls.Length > d1.Calls.Length);

            // Cannot change name
            Assert.Equal(d1.Name, d2.Name);
        }
    }
}