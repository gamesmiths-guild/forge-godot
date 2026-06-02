# OwnershipResolver

> **Type:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.OwnershipResolverResource`
> **Output Type:** `EffectOwnership`

Authors a composed `EffectOwnership` value from two nested entity resolvers.

## Authoring in Godot

- Configure the **Owner** and **Source** sections independently.
- Each section uses the standard entity resolver dropdown flow, including the renamed **Ability Owner**, **Ability Source**, and **Ability Target** entries.
- This is useful when an effect should be applied with ownership different from the current ability activation.

## Runtime Binding

At graph-build time, this resource builds the core Forge `OwnershipResolver` with the selected nested entity resolvers.

## Related Docs

- [Resolvers Reference](README.md)
- [Core OwnershipResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/ownership-resolver.md)
- [ApplyEffectNode](../nodes/apply-effect-node.md)
- [EffectNode](../nodes/effect-node.md)
