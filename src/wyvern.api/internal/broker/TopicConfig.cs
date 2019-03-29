using System;
using Akka.Configuration;
using Akka.Streams.Util;

internal static partial class Producer
{
    public class TopicConfig
    {
        public Option<string> Host { get; }
        public Option<string> Username { get; }
        public Option<string> Password { get; }
        public Option<bool> UseAmqps { get; }
        public Option<string> Entity { get; }

        public TopicConfig(Config config)
        {
            var conf = config.GetConfig("wyvern.broker.servicebus.client.default");
            Host = new Option<string>(conf.GetString("host"));
            Username = new Option<string>(conf.GetString("username"));
            Password = new Option<string>(conf.GetString("password"));
            UseAmqps = new Option<bool>(conf.GetBoolean("useAmqps"));
            Entity = new Option<string>(conf.GetString("entity"));
        }
    }


}
