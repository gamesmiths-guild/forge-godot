# AbilityStateResolver

> **Type:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.AbilityStateResolverResource`
>
> **Output Type:** `bool`

Reads a state flag from an ability for node inputs that accept a `bool`. It defaults to the ability driving the graph; provide an ability source to inspect a different ability.

## Authoring in Godot

The editor exposes a **State** dropdown and a folded **Ability** section.

- **State**: selects which flag to read — `IsActive`, `IsInhibited`, or `IsValid`.
- **Ability**: a nested resolver producing the `AbilityHandle` to inspect. Leave it empty to read from the ability driving the graph, or bind a [GetAbilityHandleResolver](get-ability-handle-resolver.md) (or an `AbilityHandle` variable) for cross-ability queries.

## Runtime Binding

At graph-build time, the Godot resource binds the core Forge `AbilityStateResolver` with the selected state type and the optional handle resolver.

## Related Docs

- [Resolvers Reference](README.md)
- [Core AbilityStateResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/ability-state-resolver.md)
- [GetAbilityHandleResolver](get-ability-handle-resolver.md)
