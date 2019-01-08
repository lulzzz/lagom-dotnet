using Akka.Configuration;
// TODO: Singleton register
internal class ReadSideConfigProvider : Provider<ReadSideConfig>
{
    public ReadSideConfigProvider(Config configuration)
    {
        var conf = new ReadSideConfig(configuration.GetConfig("wyvern.persistence.read-side"));
    }

}
