# EffectResolver

> **Type:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.EffectResolverResource`
>
> **Output Type:** `Effect` / `Effect[]`

Authors one or more `Effect` instances for effect-oriented node inputs in the Godot Statescript editor, combining a `ForgeEffectData` selection with the effect's level and ownership.

## Authoring in Godot

The editor exposes three collapsible sections: **Effect Data**, **Level**, and **Ownership**.

- **Effect Data**: use the input-row shape toggle to switch between **single** and **array** mode. In single mode assign one `ForgeEffectData` resource; in array mode assign a list of `ForgeEffectData` resources.
- **Level**: authors the effect level. It defaults to [AbilityLevelResolver](ability-level-resolver.md); pick any int-compatible resolver (for example a constant or a variable) to override it.
- **Ownership**: authors the effect owner/source pair through a nested [OwnershipResolver](ownership-resolver.md). It defaults to the current ability owner and source.
- This resolver resource is used by nodes such as [ApplyEffectNode](../nodes/apply-effect-node.md) and [EffectNode](../nodes/effect-node.md).

## Runtime Binding

At graph-build time, the Godot resource binds a lazy effect resolver wrapping the selected `ForgeEffectData` resource(s) plus the nested level and ownership resolvers. The `ForgeEffectData` is converted to runtime `EffectData` (and the core `EffectFromDataResolver` / `EffectArrayFromDataResolver` built) only when the input is resolved, so graph building (including editor-time builds such as connection loop detection) never eagerly materializes effect data. When the level or ownership section is left empty, the produced effect falls back to the current ability level and owner/source, or to level `1` and an empty ownership without an ability context.

## Related Docs

- [Resolvers Reference](README.md)
- [Core EffectFromDataResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/effect-from-data-resolver.md)
- [AbilityLevelResolver](ability-level-resolver.md)
- [OwnershipResolver](ownership-resolver.md)
- [ApplyEffectNode](../nodes/apply-effect-node.md)
- [EffectNode](../nodes/effect-node.md)
