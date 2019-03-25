using System;
using Akka.Configuration;
using Akka.Streams.Util;

internal static partial class Producer
{
    public class TopicConfig
    {
        public Option<string> Endpoint { get; }

        public TopicConfig(Config config)
        {
            Endpoint = new Option<String>(config.GetString("wyvern.broker.servicebus.client.default.endpoint"));
        }
    }


}
