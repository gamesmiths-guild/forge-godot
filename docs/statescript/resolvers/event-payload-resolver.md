# Event Payload Resolvers

> **Types:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.EventPayloadResolverResource` (raise side) and
> `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.EventPayloadOutputResolverResource` (listener side)
>
> **Output Types:** `EventPayloadRaiser` / `EventPayloadWriter`

Authors the custom typed payload of an event for the event nodes. A single `IEventPayloadProvider` serves both directions, both through the typed (non-boxing) event path: it builds and raises a typed payload on [RaiseEventNode](../nodes/raise-event-node.md) and decomposes a received payload into graph variables on [EventListenerNode](../nodes/event-listener-node.md).

## Authoring in Godot

Both sides expose a single **Provider** dropdown listing every `IEventPayloadProvider` discovered in the project assembly, plus a **(None)** option that leaves the input unbound.

To make a provider appear in either dropdown, derive from `EventPayloadProvider<TPayload>` and override `CreatePayload` (build) and `WriteOutputs` (decompose):

```csharp
using System.Collections.Generic;
using Gamesmiths.Forge.Statescript;

public sealed record HitEventPayload(int Damage, bool IsCritical);

public sealed class HitEventPayloadProvider : EventPayloadProvider<HitEventPayload>
{
    public override IReadOnlyList<EventPayloadInput> Inputs =>
        [new EventPayloadInput("Damage", typeof(int)), new EventPayloadInput("IsCritical", typeof(bool))];

    public override IReadOnlyList<EventPayloadOutput> Outputs =>
        [new EventPayloadOutput("Damage", typeof(int)), new EventPayloadOutput("IsCritical", typeof(bool))];

    public override HitEventPayload CreatePayload(GraphContext graphContext, EventPayloadInputs inputs)
        => new(inputs.Get<int>("Damage"), inputs.Get<bool>("IsCritical"));

    public override void WriteOutputs(HitEventPayload payload, EventPayloadOutputs outputs)
    {
        outputs.Set("Damage", payload.Damage);
        outputs.Set("IsCritical", payload.IsCritical);
    }
}
```

See [Custom Statescript Nodes](../nodes/custom-nodes.md#event-payload-providers) for the full provider workflow.

## Raise side: `EventPayloadResolver`

The `Payload` input of [RaiseEventNode](../nodes/raise-event-node.md); produces an `EventPayloadRaiser`. When the provider declares authored inputs, each renders below the dropdown as its own nested resolver section, so designers author the values the payload is built from. At graph-build time the `EventPayloadResolverResource` binds Forge's core `EventPayloadResolver`.

## Listener side: `EventPayloadOutputResolver`

The `Payload` input of [EventListenerNode](../nodes/event-listener-node.md); produces an `EventPayloadWriter`. Each output the provider declares renders as a graph-variable dropdown, so the received payload's values are written into the chosen variables. At graph-build time the `EventPayloadOutputResolverResource` binds Forge's core `EventPayloadOutputResolver` together with the authored output-to-variable bindings.

## Runtime Binding

The provider runs only at event time, never during graph building. Providers are discovered via reflection and shared as cached instances, so they must be stateless.

## Related Docs

- [Resolvers Reference](README.md)
- [Core EventPayloadResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/event-payload-resolver.md)
- [Custom Statescript Nodes](../nodes/custom-nodes.md#event-payload-providers)
- [RaiseEventNode](../nodes/raise-event-node.md)
- [EventListenerNode](../nodes/event-listener-node.md)
