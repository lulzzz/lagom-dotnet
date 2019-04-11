using System;
using Akka.Actor;
using Akka.Event;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace wyvern.utils
{

    public class DotNetCoreLogger : ReceiveActor, ILogReceive
    {
        ILogger<DotNetCoreLogger> Logger { get; }

        public DotNetCoreLogger()
        {
            var fac = new LoggerFactory();
            fac.AddConsole();
            Logger = fac.CreateLogger<DotNetCoreLogger>();

            Receive<Debug>(e => this.Log(Microsoft.Extensions.Logging.LogLevel.Debug, e.ToString()));
            Receive<Info>(e => this.Log(Microsoft.Extensions.Logging.LogLevel.Information, e.ToString()));
            Receive<Warning>(e => this.Log(Microsoft.Extensions.Logging.LogLevel.Warning, e.ToString()));
            Receive<Error>(e => this.Log(Microsoft.Extensions.Logging.LogLevel.Error, e.ToString()));
            Receive<InitializeLogger>(_ => this.Init(Sender));
        }

        void Init(IActorRef sender)
        {
            sender.Tell(new LoggerInitialized());
        }

        void Log(Microsoft.Extensions.Logging.LogLevel level, string message)
        {
            if (Logger != null)
                Logger.Log(level, message);
        }
    }

}