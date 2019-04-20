using Akka.Configuration;

internal static partial class Producer
{
    /// <summary>
    /// Producer configuration
    /// </summary>
    public class ProducerConfig : ClientConfig
    {
        /// <summary>
        /// Section path
        /// </summary>
        const string section = "wyvern.broker.servicebus.client.producer";

        /// <summary>
        /// Run on role
        /// </summary>
        /// <value></value>
        public string Role { get; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public ProducerConfig(Config config) : base(config, section)
        {
            Role = config.GetString("role", string.Empty);
        }
    }
}
