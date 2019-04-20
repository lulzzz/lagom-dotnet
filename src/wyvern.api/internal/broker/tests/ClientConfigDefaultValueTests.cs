using Akka.Configuration;
using Xunit;
using static Producer;

public class ClientConfigDefaultValueTests
{
    ClientConfig Config { get; }

    public ClientConfigDefaultValueTests()
    {
        Config = new ClientConfig(ConfigurationFactory.Empty, "some-section");
    }

    [Fact]
    public void clientConfig_sets_default_values_for_min_backoff()
    {
        Assert.Equal(ClientConfig.Defaults.MIN_BACKOFF, Config.MinBackoff);
    }

    [Fact]
    public void clientConfig_sets_default_values_for_max_backoff()
    {
        Assert.Equal(ClientConfig.Defaults.MAX_BACKOFF, Config.MaxBackoff);
    }

    [Fact]
    public void clientConfig_sets_default_values_for_random_backoff()
    {
        Assert.Equal(ClientConfig.Defaults.RANDOM_FACTOR, Config.RandomBackoffFactor);
    }
}
