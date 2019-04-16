using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Akka;
using Akka.Actor;
using Akka.Persistence.Query;
using Akka.Streams;
using Akka.Streams.Dsl;
using Microsoft.Extensions.Logging;
using wyvern.api.abstractions;
using wyvern.api.@internal.surfaces;
using static HelloCommand;
using static HelloEvent;

public class HelloServiceImpl : HelloService
{
    IShardedEntityRegistry Registry { get; }

    ILogger<HelloServiceImpl> Logger { get; }

    ISerializer Serializer { get; }

    ActorSystem ActorSystem { get; }

    public HelloServiceImpl(
        IShardedEntityRegistry registry,
        ILogger<HelloServiceImpl> logger,
        ISerializer serializer,
        ActorSystem actorSystem
    )
    {
        Registry = registry;
        Logger = logger;
        ActorSystem = actorSystem;
        Serializer = serializer;
    }

    public override Func<string, Func<NotUsed, Task<string>>> SayHello =>
        name =>
        async _ =>
        {
            var entity = Registry.RefFor<HelloEntity>(name);
            var response = await entity.Ask<string>(new SayHelloCommand(name));
            return response as string;
        };

    public override Func<string, Func<UpdateGreetingRequest, Task<string>>> UpdateGreeting =>
        name =>
        async req =>
        {
            var entity = Registry.RefFor<HelloEntity>(name);
            return await entity.Ask<string>(new UpdateGreetingCommand(name, req.Message));
        };

    public override Func<string, long, long, Func<WebSocket, Task>> HelloNameStream =>
        (id, st, ed) =>
        async webSocket =>
        {
            await WebSocketProducer.EntityStreamWithOffset<HelloEvent>(
                    webSocket,
                    Registry.EventStream<HelloEvent>
                )
                .Select(
                    id, st, ed,
                    (env) =>
                    {
                        var (@event, offset) = env;
                        var message = @event;
                        var obj = Newtonsoft.Json.JsonConvert.SerializeObject(message);
                        var msg = Encoding.ASCII.GetBytes(obj);
                        return msg;
                    },
                    ActorSystem.Materializer()
                );
        };

    public override Func<long, Func<WebSocket, Task>> HelloStream =>
        (st) =>
        async webSocket =>
        {
            await WebSocketProducer.StreamWithOffset<HelloEvent>(
                    webSocket,
                    Registry.EventStream<HelloEvent>
                )
                .Select(
                    st,
                    (env) =>
                    {
                        var (@event, offset) = env;
                        var message = @event;
                        var obj = Newtonsoft.Json.JsonConvert.SerializeObject(message);
                        var msg = Encoding.ASCII.GetBytes(obj);
                        return msg;
                    },
                    ActorSystem.Materializer()
                );
        };


    public override Topic<HelloEvent> GreetingsTopic() =>
        TopicProducer.SingleStreamWithOffset<HelloEvent>(
            fromOffset =>
            {
                var stream = Registry.EventStream<HelloEvent>(
                    HelloEventTag.Instance, fromOffset
                );

                stream.Select(envelope =>
                {
                    var (@event, offset) = envelope;
                    var offsetValue = ((Sequence)offset).Value.ToString();
                    var message = new DateTime();
                    return KeyValuePair.Create(message, offset);
                });

                return stream;
            }
        );
}

public class TopicMessage<T> where T : class
{
    public string MessageId { get; }
    public T Payload { get; }

    public TopicMessage(string messageId, T payload)
    {
        MessageId = messageId;
        Payload = payload;
    }
}
