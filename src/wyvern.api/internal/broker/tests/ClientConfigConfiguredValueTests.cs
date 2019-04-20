using System;
using Akka.Configuration;
using Xunit;
using static Producer;

public class ClientConfigConfiguredValueTests
{
    ClientConfig Config { get; }

    public ClientConfigConfiguredValueTests()
    {
        Config = new ClientConfig(ConfigurationFactory.ParseString(@"
            some-section {
                min = 10h
                max = 20h
                random-factor = 0.5
            }"), "some-section");
    }

    [Fact]
    public void clientConfig_sets_default_values_for_min_backoff()
    {
        Assert.Equal(TimeSpan.FromHours(10), Config.MinBackoff);
    }

    [Fact]
    public void clientConfig_sets_default_values_for_max_backoff()
    {
        Assert.Equal(TimeSpan.FromHours(20), Config.MaxBackoff);
    }

    [Fact]
    public void clientConfig_sets_default_values_for_random_backoff()
    {
        Assert.Equal(0.5d, Config.RandomBackoffFactor);
    }

}
