using System;
using Akka.Configuration;
using wyvern.utils;

internal static partial class Producer
{
    public class ClientConfig
    {
        protected Config Config { get; }

        public TimeSpan MinBackoff { get; }
        public TimeSpan MaxBackoff { get; }
        public double RandomBackoffFactor { get; }

        public ClientConfig(Config config, string section)
        {
            Config = config.GetConfig(section);

            MinBackoff = Config.GetTimeSpan("min", 1.0d.seconds());
            MaxBackoff = Config.GetTimeSpan("max", 60.0d.seconds());
            RandomBackoffFactor = Config.GetDouble("random-factor", 0.5d);
        }
    }
}
