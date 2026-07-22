# AbilityCooldownResolver

> **Type:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.AbilityCooldownResolverResource`
>
> **Output Type:** `float`

Reads a cooldown value from an ability for node inputs that accept a `float`. It defaults to the ability driving the graph; provide an ability source to inspect a different ability.

## Authoring in Godot

The editor exposes a **Data** dropdown, an optional cooldown **Tag** picker, and a folded **Ability** section.

- **Data**: selects which cooldown value to read — `RemainingTime`, `TotalTime`, or `RemainingFraction`.
- **Tag**: an optional single tag that filters which cooldown to read. Leave it empty to read the ability's default cooldown.
- **Ability**: a nested resolver producing the `AbilityHandle` to inspect. Leave it empty to read from the ability driving the graph, or bind a [GetAbilityHandleResolver](get-ability-handle-resolver.md) (or an `AbilityHandle` variable) for cross-ability queries.

## Runtime Binding

At graph-build time, the Godot resource binds a lazy resolver wrapping the core Forge `AbilityCooldownResolver`. The optional cooldown tag is requested from the tags manager only on first resolve, so graph building (including editor-time builds) never touches the runtime tags manager.

## Related Docs

- [Resolvers Reference](README.md)
- [Core AbilityCooldownResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/ability-cooldown-resolver.md)
- [GetAbilityHandleResolver](get-ability-handle-resolver.md)
