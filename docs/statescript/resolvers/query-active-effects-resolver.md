# QueryActiveEffectsResolver

> **Type:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.QueryActiveEffectsResolverResource`
>
> **Output Type:** `ActiveEffectHandle[]`

Queries the handles of the active effects on an entity, optionally filtered by an effect data resource. Feed the result into a Remove Effect node for dispel patterns, or into the array-operation resolvers.

## Authoring in Godot

The editor exposes a **Filter** picker and an **Entity** selector.

- **Filter**: an optional `ForgeEffectData` resource. When assigned, only active effects of that effect data are returned; when left empty, every active effect on the entity is returned.
- **Entity**: selects which entity to inspect — `Owner`, `Source`, `Target`, a `Variable`, or the iterated `Element` (available only inside an array operation's per-element operand). Defaults to the ability owner.

## Runtime Binding

At graph-build time, the Godot resource binds a lazy object-array resolver wrapping the core Forge `QueryActiveEffectsResolver`. When a filter is assigned, its `ForgeEffectData` is converted to runtime `EffectData` only on first resolve, so graph building (including editor-time builds) never touches the runtime managers.

## Related Docs

- [Resolvers Reference](README.md)
- [Core QueryActiveEffectsResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/query-active-effects-resolver.md)
- [Array Operations Authoring](array-operations.md)
- [ActiveEffectDataResolver](active-effect-data-resolver.md)
