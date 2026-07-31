# ActiveEffectTagQueryResolver

> **Type:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.ActiveEffectTagQueryResolverResource`
>
> **Output Type:** `bool`

Evaluates a tag query against the tags of the effect behind an `ActiveEffectHandle`. This is the predicate that makes the `Where` array operation able to filter active effects by category, so a dispel becomes *query → filter → remove* with no dedicated node.

## Authoring in Godot

The editor exposes a **query expression editor**, a **Set** dropdown, and an **Effect** section.

- **Type** / **Tags**: the same inline expression editor used by [TagQueryResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/tag-query-resolver.md) — pick an expression type (`AnyTagsMatch`, `AllTagsMatch`, `NoTagsMatch`, the `*Exact` variants, or the nested `*ExpressionsMatch` forms) and fill in the tags it looks for. Required; without it the resolver pushes an error and resolves to `false`.
- **Set**: which set of the effect's tags those tags are matched *against*.
  - `OwningTags` *(default)* — the effect's own tags **and** the tags it grants to its target.
  - `EffectTags` — the effect's identity tags (`Effect Tags` on `ForgeEffectData`) only.
  - `GrantedTags` — the tags granted through a ModifierTags component only.
- **Effect**: a nested resolver producing the `ActiveEffectHandle` to inspect — typically an Active Effect variable, or the iterated `Element` inside an array operation. Required; without it the resolver pushes an error and resolves to `false`.

Tag matching is hierarchical: an effect tagged `effect.debuff.poison` matches a query for `effect.debuff`. An effect carrying no tags at all is evaluated as an *empty* set, so negative queries such as `NoTagsMatch` still match it.

## Dispel Pattern

1. Add a **Where** array operation over a [QueryActiveEffectsResolver](query-active-effects-resolver.md) on the Active Effect input of a Remove Effect node.
2. Set the Where predicate to this resolver, with its **Effect** set to `Element`.
3. Pick the category tag in the query editor.

Every active effect on the entity whose tags match is removed; everything else is left alone.

## Runtime Binding

At graph-build time, the Godot resource builds the query expression and binds the core Forge `ActiveEffectTagQueryResolver` over the nested handle resolver.

## Related Docs

- [Resolvers Reference](README.md)
- [Core ActiveEffectTagQueryResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/active-effect-tag-query-resolver.md)
- [EffectQueryMatchResolver](effect-query-match-resolver.md)
- [QueryActiveEffectsResolver](query-active-effects-resolver.md)
- [Array Operations Authoring](array-operations.md)
