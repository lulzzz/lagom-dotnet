using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Akka;
using Akka.Persistence.Query;
using Akka.Streams;
using Akka.Streams.Dsl;
using wyvern.entity.@event;
using wyvern.entity.@event.aggregate;

public class EntityWebSocketProducer<TE>
    where TE : AbstractEvent
{
    WebSocket WebSocket { get; }
    Func<AggregateEventTag, string, Offset, Offset, Source<KeyValuePair<TE, Offset>, NotUsed>> StreamSource { get; }

    public EntityWebSocketProducer(
        WebSocket websocket,
        Func<AggregateEventTag, string, Offset, Offset, Source<KeyValuePair<TE, Offset>, NotUsed>> streamSource
    )
    {
        WebSocket = websocket;
        StreamSource = streamSource;
    }

    public async Task Select(
        string entityId,
        long startOffset,
        long endOffset,
        Func<KeyValuePair<TE, Offset>, byte[]> func,
        ActorMaterializer materializer)
    {
        var buffer = new byte[1024 * 4];
        var result = await WebSocket.ReceiveAsync(
            new ArraySegment<byte>(buffer),
            CancellationToken.None
        );

        try
        {
            var tag = AggregateEventTag.Of<TE>();
            await StreamSource.Invoke(
                    tag,
                    // TODO: check this..
                    $"{tag.Tag}|{entityId}",
                    Offset.Sequence(startOffset),
                    Offset.Sequence(endOffset)
                )
                .RunForeach((envelope) =>
                {
                    var m = func(envelope);
                    Task.Run(async () =>
                    {
                        await WebSocket.SendAsync(
                            new ArraySegment<byte>(
                                m,
                                0,
                                m.Length
                            ),
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None
                        );
                    });
                }, materializer);
        }
        catch (Exception ex)
        {
            throw;
        }

        while (!result.CloseStatus.HasValue)
        {
            result = await WebSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                CancellationToken.None
            );
        }
        await WebSocket.CloseAsync(
            result.CloseStatus.Value,
            result.CloseStatusDescription,
            CancellationToken.None
        );
    }
}