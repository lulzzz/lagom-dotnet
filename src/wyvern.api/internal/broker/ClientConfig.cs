using System;
using Akka.Configuration;
using wyvern.utils;

internal static partial class Producer
{
    /// <summary>
    /// Base configuration for any clients with backoff
    /// </summary>
    internal class ClientConfig
    {
        internal static class Defaults
        {
            public static TimeSpan MIN_BACKOFF { get; } = 1.0d.seconds();
            public static TimeSpan MAX_BACKOFF { get; } = 60.0d.seconds();
            public static double RANDOM_FACTOR { get; } = 0.5d;
        }

        /// <summary>
        /// Base config
        /// </summary>
        /// <value></value>
        protected Config Config { get; }

        /// <summary>
        /// Minimum backoff time
        /// </summary>
        /// <value></value>
        public TimeSpan MinBackoff { get; }

        /// <summary>
        /// Maximum backoff time
        /// </summary>
        /// <value></value>
        public TimeSpan MaxBackoff { get; }

        /// <summary>
        /// Random backoff factor
        /// </summary>
        /// <value></value>
        public double RandomBackoffFactor { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="config">Config root</param>
        /// <param name="section">Section name</param>
        public ClientConfig(Config config, string section)
        {
            Config = config.GetConfig(section);
            if (Config == null || Config.IsEmpty) Config = Config.Empty;

            MinBackoff = Config.GetTimeSpan("min", Defaults.MIN_BACKOFF);
            MaxBackoff = Config.GetTimeSpan("max", Defaults.MAX_BACKOFF);
            RandomBackoffFactor = Config.GetDouble("random-factor", Defaults.RANDOM_FACTOR);
        }
    }
}
