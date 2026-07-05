# Property Resolvers

Forge for Godot uses the core Forge resolver set and adds Godot-facing resolver resources when visual authoring needs resource-specific behavior.

Use the **core Forge documentation** for runtime resolver behavior and API details. Use the pages in this folder when Godot authoring adds editor, resource, or binding details.

## Core Resolver Reference

| Category | Core Docs | Notes |
|----------|-----------|-------|
| Array Operations | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/README.md#array-operations) | LINQ-style access, transform, and reduction resolvers over value and object arrays, plus the element (lambda) resolvers. |
| Boolean Expressions | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/README.md#boolean-expressions) | Logical composition and comparison resolvers. |
| Built-in Resolvers | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/README.md#built-in-resolvers) | Constants, arrays, variables, activation data, attributes, tags, and other general-purpose resolvers. |
| Entity Resolvers | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/README.md#entity-resolvers) | Resolvers that read owner/source/target entities and entity-typed values. |
| Effect Resolvers | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/README.md#effect-resolvers) | Runtime `EffectFromDataResolver`, `EffectArrayFromDataResolver`, and the effect-variable resolvers. |
| Object Utilities | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/README.md#object-utilities) | Validity (non-null) and reference-identity checks over object-backed variables. |
| Math | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/README.md#math) | Numeric, vector, interpolation, and magnitude helpers. |
| Random | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/README.md#random) | Random scalar and spatial resolvers. |
| Spatial Math | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/README.md#spatial-math) | Direction, angle, quaternion, plane, and vector-space helpers. |

## Godot Resolver Resources

These pages cover authoring details that the Godot editor adds on top of the core resolver behavior.

| Resolver | Output Type | Description |
|----------|-------------|-------------|
| [AbilityLevelResolver](ability-level-resolver.md) | `int` | Authors the current ability level as a node input. |
| [AbilityOwnershipResolver](ability-ownership-resolver.md) | `EffectOwnership` | Authors the current ability owner/source pair as a node input. |
| [CueCustomParametersResolver](cue-custom-parameters-resolver.md) | `Dictionary<StringKey, object>` | Selects an `ICueCustomParametersProvider` to author the `CueParameters.CustomParameters` bag for the cue nodes. |
| [EffectContextDataResolver](effect-context-data-resolver.md) | `EffectApplicationContext` | Selects an `IEffectContextDataProvider` to pass custom context data into effect applications. |
| [EffectResolver](effect-resolver.md) | `Effect` / `Effect[]` | Authors `Effect` instances (effect data + level + ownership) for `ApplyEffectNode` and `EffectNode`. |
| [EventPayloadOutputResolver](event-payload-resolver.md#listener-side-eventpayloadoutputresolver) | `EventPayloadWriter` | Selects an `IEventPayloadProvider` to write a received payload to graph variables for `EventListenerNode`. |
| [EventPayloadResolver](event-payload-resolver.md#raise-side-eventpayloadresolver) | `EventPayloadRaiser` | Selects an `IEventPayloadProvider` to build and raise a typed event payload for `RaiseEventNode`. |
| [IsValidResolver](is-valid-resolver.md) | `bool` | Authors a validity (non-null) check over an object-backed variable of any registered type. |
| [ObjectEqualsResolver](object-equals-resolver.md) | `bool` | Authors a reference-identity check between two object-backed variables of any registered type. |
| [OwnershipResolver](ownership-resolver.md) | `EffectOwnership` | Composes effect ownership from two nested entity resolvers. |
| [TagResolver](tag-resolver.md) | `Tag` | Selects one or more registered tags for any tag input (e.g. the cue nodes). |

## Authoring Guides

The array-operation resolver family (`Where`, `Order By`, `Take`, `Select`, `Count`, `Any`, `First`, ...) and the element (lambda) resolvers share one editor authoring model rather than per-resolver behavior, so they are documented as a single guide (their per-operation runtime behavior lives in the [core Array Operations reference](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/README.md#array-operations)).

- [Array Operations Authoring](array-operations.md) — nested source, per-element (lambda) operands and element resolvers, iteration scope, and the value/object lanes.

## Related Docs

- [Variables and Data](../variables.md)
- [Custom Resolvers](../custom-resolvers.md)
- [Resolver Template](../templates/resolver-template.md)
