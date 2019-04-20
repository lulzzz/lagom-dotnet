using System;
using Xunit;
using wyvern.utils;

namespace wyvern.utils.tests
{
    public class DoubleExtensionTests
    {
        [Theory]
        [InlineData(0.3d)]
        [InlineData(5.12d)]
        [InlineData(1d)]
        public void double_to_seconds(double input)
        {
            Assert.Equal(TimeSpan.FromSeconds(input), input.seconds());
        }

        [Theory]
        [InlineData(0.3d)]
        [InlineData(5.12d)]
        [InlineData(1d)]
        public void double_to_minutes(double input)
        {
            Assert.Equal(TimeSpan.FromMinutes(input), input.minutes());
        }

        [Theory]
        [InlineData(0.3d)]
        [InlineData(5.12d)]
        [InlineData(1d)]
        public void double_to_hours(double input)
        {
            Assert.Equal(TimeSpan.FromHours(input), input.hours());
        }

        [Theory]
        [InlineData(0.3d)]
        [InlineData(5.12d)]
        [InlineData(1d)]
        public void double_to_days(double input)
        {
            Assert.Equal(TimeSpan.FromDays(input), input.days());
        }
    }
}
