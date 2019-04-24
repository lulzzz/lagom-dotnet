using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Akka.Actor;
using Akka.Configuration;
using Akka.TestKit.Xunit.Internals;
using Akka.TestKit;

namespace wyvern.api.tests
{
    public class ActorSystemFixture : IDisposable
    {
        public ActorSystem ActorSystem { get; }

        public ActorSystemFixture()
        {
            ActorSystem = ActorSystem.Create("test");
        }

        public void Dispose()
        {
            ActorSystem.Terminate();
            ActorSystem.WhenTerminated.Wait();
        }
    }
}