# ActiveEffectSourceResolver / ActiveEffectOwnerResolver

> **Types:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.ActiveEffectSourceResolverResource`, `...ActiveEffectOwnerResolverResource`
>
> **Output Type:** `IForgeEntity?`

Read the two ends of an effect's ownership from an `ActiveEffectHandle` produced by a nested resolver. Being entity-typed, they compose as nested inputs to entity-aware resolvers (such as `AttributeResolver` or `TagQueryResolver`) and to entity node inputs, rather than binding directly to a value property.

| Resolver | Reads | Means |
|---|---|---|
| **Active Effect Source** | `Ownership.Source` | *What actually caused* the effect — the weapon, the projectile, the trap. |
| **Active Effect Owner** | `Ownership.Owner` | *Who triggered the action* that caused the effect. |

Together with [ActiveEffectTargetResolver](active-effect-target-resolver.md) they complete the set: a graph holding a handle can now reach every entity involved in the effect.

## Authoring in Godot

Both editors expose a single **Effect** section.

- **Effect**: a nested resolver producing the `ActiveEffectHandle` to inspect — typically an Active Effect variable, a [QueryActiveEffectsResolver](query-active-effects-resolver.md) element, or the iterated `Element` inside an array operation. This input is required; if it is missing or does not produce an `ActiveEffectHandle`, the resolver pushes an error and falls back to the ability owner.

Invalid or expired handles resolve to `null`.

## Runtime Binding

At graph-build time, each Godot resource binds the matching core Forge resolver (an `IEntityResolver`) over the nested handle resolver.

## Related Docs

- [Resolvers Reference](README.md)
- [Core ActiveEffectSourceResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/active-effect-source-resolver.md)
- [Core ActiveEffectOwnerResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/active-effect-owner-resolver.md)
- [ActiveEffectTargetResolver](active-effect-target-resolver.md)
