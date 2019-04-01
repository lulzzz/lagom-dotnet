using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Akka;
using Akka.Actor;
using Akka.Persistence.Query;
using Akka.Streams;
using Akka.Streams.Dsl;
using Microsoft.Extensions.Logging;
using wyvern.api.abstractions;
using wyvern.api.@internal.surfaces;
using wyvern.entity.@event.aggregate;
using static HelloCommand;
using static HelloEvent;

public class HelloServiceImpl : HelloService
{
    IShardedEntityRegistry Registry { get; }

    ILogger<HelloServiceImpl> Logger { get; }

    ActorSystem ActorSystem { get; }

    public HelloServiceImpl(
            IShardedEntityRegistry registry,
            ILogger<HelloServiceImpl> logger,
            ActorSystem actorSystem
        )
    {
        Registry = registry;
        Logger = logger;
        ActorSystem = actorSystem;
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

    public override Func<Func<WebSocket, Task>> HelloStream =>
        () =>
        async webSocket =>
        {
            var buffer = new byte[1024 * 4];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            await Registry.EventStream<HelloEvent>(
                HelloEventTag.Instance,
                Offset.NoOffset()
            )
            .RunForeach(
                (KeyValuePair<HelloEvent, Offset> envelope) =>
                {
                    var (@event, offset) = envelope;
                    var message = @event;
                    var obj = Newtonsoft.Json.JsonConvert.SerializeObject(message);
                    var msg = Encoding.ASCII.GetBytes(obj);

                    Task.Run(async () =>
                    {
                        await webSocket.SendAsync(
                                new ArraySegment<byte>(
                                    msg,
                                    0,
                                    msg.Length
                                ),
                                WebSocketMessageType.Text,
                                true,
                                CancellationToken.None
                            );
                    });
                },
                ActorMaterializer.Create(ActorSystem)
            );

            while (!result.CloseStatus.HasValue)
            {
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        };

    public override Topic<HelloEvent> GreetingsTopic() =>
        TopicProducer.SingleStreamWithOffset<HelloEvent>(
            fromOffset => Registry.EventStream<HelloEvent>(
                HelloEventTag.Instance, fromOffset
            )
            .Select(envelope =>
            {
                var (@event, offset) = envelope;
                var message = @event;
                return KeyValuePair.Create(message, offset);
            })
        );
}
