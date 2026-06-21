# EventListenerNode

> **Runtime Type:** `Gamesmiths.Forge.Statescript.Nodes.State.EventListenerNode`

Subscribes to event tags on an entity's event bus while active and emits the `OnEvent` port each time a matching event fires.

Use the core Forge docs for runtime behavior and lifecycle details. This page covers the Godot authoring details.

## Authoring in Godot

- **Inputs:** the `Event Tags` input is authored through [TagResolver](../resolvers/tag-resolver.md) (check one or more registered tags; the node subscribes to each), and the `Listen On` input selects the entity whose `Events` bus is observed via the standard entity resolver flow (for example the ability owner).
- **Output Variables:** bind the built-in `Source`, `Target` (entity) and `Magnitude` (float) outputs to graph variables to capture the received event's fields. Each output dropdown lists only graph variables of the matching type.
- **Payload outputs:** the **Payload** row in the Output Variables section offers a provider dropdown listing every `IEventPayloadProvider`. Pick one and each of its declared outputs gets its own graph-variable dropdown (filtered to that output's type, e.g. `int`/`bool`/`float`); the received payload's values are written into the bound variables when an event fires. See [EventPayloadResolver](../resolvers/event-payload-resolver.md).
- The node exposes an `OnEvent` output port that fires each time a matching event is raised. It has no timer; place it as a subgraph of another state node so it lives for that state's duration, and route `OnEvent` into the reaction you want.

## Related Docs

- [Nodes Reference](README.md)
- [Core EventListenerNode](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/state/event-listener-node.md)
- [TagResolver](../resolvers/tag-resolver.md)
- [EventPayloadResolver](../resolvers/event-payload-resolver.md)
- [RaiseEventNode](raise-event-node.md)
