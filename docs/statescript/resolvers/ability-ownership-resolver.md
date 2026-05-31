# AbilityOwnershipResolver

> **Type:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.AbilityOwnershipResolverResource`
> **Output Type:** `EffectOwnership`

Authors the current ability owner/source pair for node inputs that accept an `EffectOwnership`.

## Authoring in Godot

- No extra configuration is required.
- Select **Ability Ownership** in the resolver dropdown to bind the current ability owner and source directly.
- This is the most direct way to keep `ApplyEffectNode.Ownership` or `EffectNode.Ownership` aligned with the current ability activation.

## Runtime Binding

At graph-build time, this resource binds the core Forge `AbilityOwnershipResolver`.

## Related Docs

- [Resolvers Reference](README.md)
- [Core AbilityOwnershipResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/ability-ownership-resolver.md)
- [ApplyEffectNode](../nodes/apply-effect-node.md)
- [EffectNode](../nodes/effect-node.md)
