# QueryActiveEffectsResolver

> **Type:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.QueryActiveEffectsResolverResource`
>
> **Output Type:** `ActiveEffectHandle[]`

Queries the handles of the active effects on an entity, filtered by an effect query. Feed the result into a Remove Effect node for dispel patterns, or into the array-operation resolvers.

## Authoring in Godot

The editor exposes a **Query** picker and an **Entity** selector.

- **Query**: an optional [`ForgeEffectQuery`](effect-query-match-resolver.md#the-forgeeffectquery-resource) resource. Leave it empty to return every active effect on the entity. Set the query's **Effect Definition** to select every application of one specific effect; set its tag queries to select by category instead — every curse, every effect that touches health, everything a given kind of source applied.
- **Entity**: selects which entity to inspect — `Owner`, `Source`, `Target`, a `Variable`, or the iterated `Element` (available only inside an array operation's per-element operand). Defaults to the ability owner.

Put the filter in the query when it is expressible as one, and use a `Where` array operation with an [ActiveEffectTagQueryResolver](active-effect-tag-query-resolver.md) when the predicate has to read per-element runtime state the query cannot express (remaining duration, stack count, inhibition).

## Runtime Binding

At graph-build time, the Godot resource binds a lazy object-array resolver wrapping the core Forge `QueryActiveEffectsResolver`. The assigned `ForgeEffectQuery` is converted to a runtime `EffectQuery` only on first resolve, so graph building (including editor-time builds) never touches the runtime managers.

## Related Docs

- [Resolvers Reference](README.md)
- [Core QueryActiveEffectsResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/query-active-effects-resolver.md)
- [Array Operations Authoring](array-operations.md)
- [ActiveEffectDataResolver](active-effect-data-resolver.md)
- [ActiveEffectTagQueryResolver](active-effect-tag-query-resolver.md)
- [EffectQueryMatchResolver](effect-query-match-resolver.md)
