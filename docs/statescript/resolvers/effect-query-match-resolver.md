# EffectQueryMatchResolver

> **Type:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.EffectQueryMatchResolverResource`
>
> **Output Type:** `bool`

Matches a full `ForgeEffectQuery` against the effect behind an `ActiveEffectHandle`. Use it when the filter needs more than tags — a specific effect data, source tag requirements, or a modified attribute. For the common tag-only case, [ActiveEffectTagQueryResolver](active-effect-tag-query-resolver.md) is cheaper to configure.

## Authoring in Godot

The editor exposes a **Query** picker and an **Effect** section.

- **Query**: a `ForgeEffectQuery` resource. When left empty the query is empty, and an empty query matches every effect.
- **Effect**: a nested resolver producing the `ActiveEffectHandle` to inspect — typically an Active Effect variable, or the iterated `Element` inside an array operation. Required; without it the resolver pushes an error and resolves to `false`.

## The ForgeEffectQuery Resource

Create it from the resource menu (`ForgeEffectQuery`). Every field is optional and all filled-in fields are combined with **AND**.

| Field | Matched against |
|---|---|
| **Effect Definition** | The exact `ForgeEffectData` the effect was built from. |
| **Tag Queries → Effect Tag Query** | The effect's own identity tags. |
| **Tag Queries → Granted Tag Query** | The tags the effect grants to its target. |
| **Tag Queries → Owning Tag Query** | Both sets together. |
| **Source Requirements → Required / Ignored Tags, Tag Query** | The tags of the entity that applied the effect. |
| **Modifiers → Modifying Attribute** | Any of the effect's modifiers, by attribute name (e.g. `CombatAttributeSet.CurrentHealth`). Leave empty to ignore. |

The runtime `EffectQuery` also supports matching a specific source entity instance and an arbitrary predicate. Neither can be authored as a resource, so both stay code-only.

## Runtime Binding

At graph-build time, the Godot resource binds a lazy property resolver wrapping the core Forge `EffectQueryMatchResolver`. The `ForgeEffectQuery` is converted to a runtime `EffectQuery` only on first resolve, so graph building (including editor-time builds) never touches the runtime managers.

## Related Docs

- [Resolvers Reference](README.md)
- [Core EffectQueryMatchResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/effect-query-match-resolver.md)
- [Core EffectQuery](https://github.com/gamesmiths-guild/forge/blob/main/docs/effects/README.md#effectquery)
- [ActiveEffectTagQueryResolver](active-effect-tag-query-resolver.md)
- [QueryActiveEffectsResolver](query-active-effects-resolver.md)
