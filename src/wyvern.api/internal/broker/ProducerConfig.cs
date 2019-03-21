using Akka.Configuration;

internal static partial class Producer
{
    public class ProducerConfig : ClientConfig
    {
        const string section = "wyvern.broker.servicebus.client.producer";

        public string Role { get; }

        public ProducerConfig(Config config) : base(config, section)
        {
            Role = config.GetString("role", "");
        }
    }
}
