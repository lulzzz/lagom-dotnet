using System;
using System.Threading.Tasks;
using Akka;
using wyvern.api;
using wyvern.api.abstractions;
using wyvern.api.@internal.surfaces;

public abstract class HelloService : Service
{
    public class UpdateGreetingRequest
    {
        public string Message { get; set; }
    }

    public abstract Func<string, Func<NotUsed, Task<string>>> SayHello { get; }

    public abstract Func<string, Func<UpdateGreetingRequest, Task<string>>> UpdateGreeting { get; }

    public abstract Topic<HelloEvent> GreetingsTopic();

    public override IDescriptor Descriptor =>
        Named("HelloService")
            .WithCalls(
                RestCall(Method.GET, "/api/hello/{name}", SayHello),
                RestCall(Method.POST, "/api/hello/{name}", UpdateGreeting)
            )
            .WithTopics(
                Topic("greetings-service", GreetingsTopic)
            );
}
