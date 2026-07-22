# ActiveEffectDataResolver

> **Type:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.ActiveEffectDataResolverResource`
>
> **Output Type:** `double` / `int` / `bool`

Reads a selected runtime value (remaining duration, stack count, level, ...) from an `ActiveEffectHandle` produced by a nested resolver — typically an Active Effect variable or a [QueryActiveEffectsResolver](query-active-effects-resolver.md) element.

## Authoring in Godot

The editor exposes a **Data** dropdown and an **Effect** section.

- **Data**: selects which value to read — `RemainingDuration`, `TotalDuration`, `RemainingFraction`, `StackCount`, `Level`, `ExecutionCount`, `Period`, `IsInhibited`, or `IsValid`. The output type follows the selection (`double`, `int`, or `bool`).
- **Effect**: a nested resolver producing the `ActiveEffectHandle` to inspect. This input is required; if it is missing or does not produce an `ActiveEffectHandle`, the resolver pushes an error and falls back to a default value.

## Runtime Binding

At graph-build time, the Godot resource binds the core Forge `ActiveEffectDataResolver` with the nested handle resolver and the selected data type.

## Related Docs

- [Resolvers Reference](README.md)
- [Core ActiveEffectDataResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/active-effect-data-resolver.md)
- [QueryActiveEffectsResolver](query-active-effects-resolver.md)
