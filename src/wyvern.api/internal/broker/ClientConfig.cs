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

            MinBackoff = Config.GetTimeSpan("min", 1.0d.seconds());
            MaxBackoff = Config.GetTimeSpan("max", 60.0d.seconds());
            RandomBackoffFactor = Config.GetDouble("random-factor", 0.5d);
        }
    }
}
