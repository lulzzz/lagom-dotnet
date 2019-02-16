using Akka.Configuration;

// TODO: Singleton register
namespace wyvern.api.@internal.readside
{
    internal class ReadSideConfigProvider : Provider<ReadSideConfig>
    {
        public ReadSideConfigProvider(Config configuration)
        {
            var conf = new ReadSideConfig(configuration.GetConfig("wyvern.persistence.read-side"));
        }

    }
}
