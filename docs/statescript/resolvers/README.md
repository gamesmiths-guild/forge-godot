# Property Resolvers

Forge for Godot uses the core Forge resolver set and adds Godot-facing resolver resources when visual authoring needs resource-specific behavior.

Use the **core Forge documentation** for runtime resolver behavior and API details. Use the pages in this folder when Godot authoring adds editor, resource, or binding details.

## Core Resolver Reference

| Category | Core Docs | Notes |
|----------|-----------|-------|
| Built-in Resolvers | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/README.md#built-in-resolvers) | Constants, arrays, variables, activation data, attributes, tags, and other general-purpose resolvers. |
| Entity Resolvers | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/README.md#entity-resolvers) | Resolvers that read owner/source/target entities and entity-typed values. |
| Effect Resolvers | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/README.md#effect-resolvers) | Runtime `EffectFromDataResolver`, `EffectArrayFromDataResolver`, and the effect-variable resolvers. |
| Boolean Expressions | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/README.md#boolean-expressions) | Logical composition and comparison resolvers. |
| Math | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/README.md#math) | Numeric, vector, interpolation, and magnitude helpers. |
| Spatial Math | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/README.md#spatial-math) | Direction, angle, quaternion, plane, and vector-space helpers. |
| Random | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/README.md#random) | Random scalar and spatial resolvers. |

## Godot Resolver Resources

| Resolver | Output Type | Description |
|----------|-------------|-------------|
| [AbilityLevelResolver](ability-level-resolver.md) | `int` | Authors the current ability level as a node input. |
| [AbilityOwnershipResolver](ability-ownership-resolver.md) | `EffectOwnership` | Authors the current ability owner/source pair as a node input. |
| [CueCustomParametersResolver](cue-custom-parameters-resolver.md) | `Dictionary<StringKey, object>` | Selects an `ICueCustomParametersProvider` to author the `CueParameters.CustomParameters` bag for the cue nodes. |
| [EffectContextDataResolver](effect-context-data-resolver.md) | `EffectApplicationContext` | Selects an `IEffectContextDataProvider` to pass custom context data into effect applications. |
| [EffectResolver](effect-resolver.md) | `Effect` / `Effect[]` | Authors `Effect` instances (effect data + level + ownership) for `ApplyEffectNode` and `EffectNode`. |
| [OwnershipResolver](ownership-resolver.md) | `EffectOwnership` | Composes effect ownership from two nested entity resolvers. |
| [TagResolver](tag-resolver.md) | `Tag` | Selects one or more registered tags for any tag input (e.g. the cue nodes). |

## Related Docs

- [Variables and Data](../variables.md)
- [Custom Resolvers](../custom-resolvers.md)
- [Resolver Template](../templates/resolver-template.md)
