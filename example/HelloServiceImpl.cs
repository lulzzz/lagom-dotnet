using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka;
using Akka.Streams.Dsl;
using wyvern.api.abstractions;
using wyvern.api.@internal.surfaces;
using wyvern.entity.@event.aggregate;
using static HelloCommand;
using static HelloEvent;

public class HelloServiceImpl : HelloService
{
    IShardedEntityRegistry Registry { get; }

    public HelloServiceImpl(IShardedEntityRegistry registry)
        => Registry = registry;

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

    public override Topic<HelloEvent> GreetingsTopic() =>
        TopicProducer.SingleStreamWithOffset<HelloEvent>(
            fromOffset => Registry.EventStream<HelloEvent>(
                ArticleWebsiteDisplayRuleEventTag.Instance, fromOffset
            )
            .Select(envelope =>
            {
                var (@event, offset) = envelope;
                var material = @event;
                // TODO: send material to service bus
                return KeyValuePair.Create(material, offset);
            })
        );
}
