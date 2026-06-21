# RaiseEventNode

> **Runtime Type:** `Gamesmiths.Forge.Statescript.Nodes.Action.RaiseEventNode`

Raises an event on one or more target entities' event buses.

Use the core Forge docs for runtime behavior and port details. This page covers the Godot authoring details.

## Authoring in Godot

- The node has an `Event Tags` input and a `Target` input, plus optional `Source`, `Magnitude`, and `Payload` inputs.
- The `Event Tags` input is authored through [TagResolver](../resolvers/tag-resolver.md): check one or more registered tags. They are combined into the event's tag container, so a single raise carries every selected tag.
- The `Target` input uses the standard entity resolver flow and supports single or array bindings via the input-row shape toggle. The event is raised on every target.
- `Source` (entity) and `Magnitude` (float) are optional and map to `EventData.Source` / `EventData.EventMagnitude`.
- `Payload` is optional and authored through [EventPayloadResolver](../resolvers/event-payload-resolver.md): pick an `IEventPayloadProvider` to build and raise a typed payload (with no boxing) from the graph state. Binding a provider makes the node raise the provider's typed `EventData<TPayload>`, so a typed listener with the same provider receives it.
- The node has no output, events are addressed entirely by tag.

## Related Docs

- [Nodes Reference](README.md)
- [Core RaiseEventNode](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/action/raise-event-node.md)
- [TagResolver](../resolvers/tag-resolver.md)
- [EventPayloadResolver](../resolvers/event-payload-resolver.md)
- [EventListenerNode](event-listener-node.md)
