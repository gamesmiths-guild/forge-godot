# EffectDataResolver

> **Type:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.EffectDataResolverResource`
>
> **Output Type:** `EffectData` / `EffectData[]`

Authors one or more `ForgeEffectData` resources for effect-oriented node inputs in the Godot Statescript editor.

## Authoring in Godot

- Use the input-row shape toggle to switch between **single** and **array** mode.
- In single mode, assign one `ForgeEffectData` resource.
- In array mode, assign a list of `ForgeEffectData` resources.
- This resolver resource is used by nodes such as [ApplyEffectNode](../nodes/apply-effect-node.md) and [EffectNode](../nodes/effect-node.md).

## Runtime Binding

At graph-build time, the Godot resource binds the core Forge `EffectDataResolver` / `EffectDataArrayResolver` behavior.

The conversion from `ForgeEffectData` to runtime `EffectData` is deferred through internal lazy runtime resolvers so editor-time graph validation does not eagerly call `GetEffectData()`.

## Related Docs

- [Resolvers Reference](README.md)
- [Core EffectDataResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/effect-data-resolver.md)
- [Core EffectDataArrayResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/effect-data-array-resolver.md)
- [ApplyEffectNode](../nodes/apply-effect-node.md)
- [EffectNode](../nodes/effect-node.md)
