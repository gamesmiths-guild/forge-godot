# EffectNode

> **Runtime Type:** `Gamesmiths.Forge.Statescript.Nodes.State.EffectNode`

Applies one or more effects while the node remains active and removes still-active instances on deactivation.

Use the core Forge docs for runtime behavior and lifecycle details. This page covers the Godot authoring details.

## Authoring in Godot

- The `Effect` input is authored through [EffectDataResolver](../resolvers/effect-data-resolver.md).
- The input-row shape toggle switches the `Effect` binding between **single** and **array** mode.
- The `Target` input uses the standard entity resolver flow and also supports single or array bindings.
- The `Level` input uses normal scalar resolver authoring. Use [AbilityLevelResolver](../resolvers/ability-level-resolver.md) to read the current ability level, or bind another int-compatible resolver when you need an explicit override.
- The `Ownership` input uses object-backed resolver authoring. Use [AbilityOwnershipResolver](../resolvers/ability-ownership-resolver.md) for the current ability owner/source pair, or [OwnershipResolver](../resolvers/ownership-resolver.md) to compose a custom owner and source from nested entity resolvers.
- The node follows the same effect/entity cross-product authoring flow as [ApplyEffectNode](apply-effect-node.md).

## Related Docs

- [Nodes Reference](README.md)
- [Core EffectNode](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/state/effect-node.md)
- [AbilityLevelResolver](../resolvers/ability-level-resolver.md)
- [AbilityOwnershipResolver](../resolvers/ability-ownership-resolver.md)
- [EffectDataResolver](../resolvers/effect-data-resolver.md)
- [OwnershipResolver](../resolvers/ownership-resolver.md)
