# ActiveEffectTargetResolver

> **Type:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.ActiveEffectTargetResolverResource`
>
> **Output Type:** `IForgeEntity?`

Reads the entity an active effect is applied to, from an `ActiveEffectHandle` produced by a nested resolver. Being entity-typed, it composes as a nested input to entity-aware resolvers (such as `AttributeResolver` or `TagQueryResolver`) rather than binding directly to a node property.

## Authoring in Godot

The editor exposes a single **Effect** section.

- **Effect**: a nested resolver producing the `ActiveEffectHandle` to inspect — typically an Active Effect variable or a [QueryActiveEffectsResolver](query-active-effects-resolver.md) element. This input is required; if it is missing or does not produce an `ActiveEffectHandle`, the resolver pushes an error and falls back to the ability owner.

## Runtime Binding

At graph-build time, the Godot resource binds the core Forge `ActiveEffectTargetResolver` (an `IEntityResolver`) over the nested handle resolver.

## Related Docs

- [Resolvers Reference](README.md)
- [Core ActiveEffectTargetResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/active-effect-target-resolver.md)
- [QueryActiveEffectsResolver](query-active-effects-resolver.md)
