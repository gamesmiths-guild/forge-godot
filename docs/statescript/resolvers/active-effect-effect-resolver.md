# ActiveEffectEffectResolver

> **Type:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.ActiveEffectEffectResolverResource`
>
> **Output Type:** `Effect?`

Reads the live `Effect` instance behind an `ActiveEffectHandle` produced by a nested resolver, bridging the handle lane back to the effect lane (for example to feed a [SetByCallerMagnitudeResolver](set-by-caller-magnitude-resolver.md)).

## Authoring in Godot

The editor exposes a single **Handle** section.

- **Handle**: a nested resolver producing the `ActiveEffectHandle` to inspect — typically an Active Effect variable or a [QueryActiveEffectsResolver](query-active-effects-resolver.md) element. This input is required; if it is missing or does not produce an `ActiveEffectHandle`, the resolver pushes an error and binds nothing.

## Runtime Binding

At graph-build time, the Godot resource binds the core Forge `ActiveEffectEffectResolver` over the nested handle resolver as an object-backed resolver.

## Related Docs

- [Resolvers Reference](README.md)
- [Core ActiveEffectEffectResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/active-effect-effect-resolver.md)
- [QueryActiveEffectsResolver](query-active-effects-resolver.md)
- [SetByCallerMagnitudeResolver](set-by-caller-magnitude-resolver.md)
