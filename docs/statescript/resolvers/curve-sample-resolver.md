# CurveSampleResolver

> **Type:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.CurveSampleResolverResource`
>
> **Output Type:** `float`

Samples a Godot `Curve` resource at a resolved position, for node inputs that accept a `float`. This is the Godot binding for the core `CurveSampleResolver`'s `ICurve` input, letting designers author response curves as native `Curve` assets.

## Authoring in Godot

The editor exposes a **Curve** picker and a **Time** section.

- **Curve**: assign a Godot `Curve` resource to sample.
- **Time**: a nested resolver producing the sample position along the curve. When left empty, the position defaults to `0`.

## Runtime Binding

At graph-build time, the Godot resource wraps the assigned `Curve` in a `ForgeCurve` (an `ICurve`) and binds the core Forge `CurveSampleResolver` with the nested time resolver.

## Related Docs

- [Resolvers Reference](README.md)
- [Core CurveSampleResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/curve-sample-resolver.md)
