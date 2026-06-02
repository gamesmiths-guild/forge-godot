# ApplyEffectNode

> **Runtime Type:** `Gamesmiths.Forge.Statescript.Nodes.Action.ApplyEffectNode`

Applies one or more effects to one or more targets.

Use the core Forge docs for runtime behavior and port details. This page covers the Godot authoring details.

## Authoring in Godot

- The `Effect` input is authored through [EffectDataResolver](../resolvers/effect-data-resolver.md).
- The input-row shape toggle switches the `Effect` binding between **single** and **array** mode.
- The `Target` input uses the standard entity resolver flow and also supports single or array bindings.
- The `Level` input uses normal scalar resolver authoring. Use [AbilityLevelResolver](../resolvers/ability-level-resolver.md) to keep the current ability level explicit, or bind any other int-compatible resolver when you need an override.
- The `Ownership` input uses object-backed resolver authoring. Use [AbilityOwnershipResolver](../resolvers/ability-ownership-resolver.md) for the current ability owner/source pair, or [OwnershipResolver](../resolvers/ownership-resolver.md) to compose a custom owner and source from nested entity resolvers.
- All four effect/entity combinations are supported: single/single, single/array, array/single, and array/array.

## Related Docs

- [Nodes Reference](README.md)
- [Core ApplyEffectNode](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/action/apply-effect-node.md)
- [AbilityLevelResolver](../resolvers/ability-level-resolver.md)
- [AbilityOwnershipResolver](../resolvers/ability-ownership-resolver.md)
- [EffectDataResolver](../resolvers/effect-data-resolver.md)
- [OwnershipResolver](../resolvers/ownership-resolver.md)
