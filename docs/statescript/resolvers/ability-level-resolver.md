# AbilityLevelResolver

> **Type:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.AbilityLevelResolverResource`
> **Output Type:** `int`

Authors the current ability level for node inputs that accept an `int`.

## Authoring in Godot

- No extra configuration is required.
- Select **Ability Level** in the resolver dropdown to bind the current ability level directly.
- This is a convenient explicit binding for `ApplyEffectNode.Level` and `EffectNode.Level`.

## Runtime Binding

At graph-build time, this resource binds the core Forge `AbilityLevelResolver`.

## Related Docs

- [Resolvers Reference](README.md)
- [Core AbilityLevelResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/ability-level-resolver.md)
- [ApplyEffectNode](../nodes/apply-effect-node.md)
- [EffectNode](../nodes/effect-node.md)
