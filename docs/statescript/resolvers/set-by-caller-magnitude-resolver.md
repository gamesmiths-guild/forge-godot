# SetByCallerMagnitudeResolver

> **Type:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.SetByCallerMagnitudeResolverResource`
>
> **Output Type:** `float`

Reads the SetByCaller magnitude currently stored on an `Effect` for an identifier tag, for node inputs that accept a `float`.

## Authoring in Godot

The editor exposes an identifier **Tag** picker and an **Effect** section.

- **Tag**: the SetByCaller identifier tag whose magnitude to read.
- **Effect**: a nested resolver producing the `Effect` to inspect — for example an Effect variable or an [ActiveEffectEffectResolver](active-effect-effect-resolver.md).

Both the identifier tag and the effect source are required; if either is missing, the resolver pushes an error and falls back to a default `float`.

## Runtime Binding

At graph-build time, the Godot resource binds a lazy resolver wrapping the core Forge `SetByCallerMagnitudeResolver`. The identifier tag is requested from the tags manager only on first resolve, so graph building (including editor-time builds) never touches the runtime tags manager.

## Related Docs

- [Resolvers Reference](README.md)
- [Core SetByCallerMagnitudeResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/set-by-caller-magnitude-resolver.md)
- [ActiveEffectEffectResolver](active-effect-effect-resolver.md)
