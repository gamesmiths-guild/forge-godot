# CanActivateAbilityResolver

> **Type:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.CanActivateAbilityResolverResource`
>
> **Output Type:** `bool`

Checks whether an ability can currently activate — evaluating cooldowns, costs, and tag requirements — for node inputs that accept a `bool`. It defaults to the ability driving the graph; provide an ability source to inspect a different ability.

## Authoring in Godot

The editor exposes a folded **Target** section and a folded **Ability** section.

- **Target**: an optional nested entity resolver used as the activation target for target tag requirement checks. Leave it empty to skip target-based requirements.
- **Ability**: a nested resolver producing the `AbilityHandle` to inspect. Leave it empty to check the ability driving the graph, or bind a [GetAbilityHandleResolver](get-ability-handle-resolver.md) (or an `AbilityHandle` variable) for cross-ability queries.

## Runtime Binding

At graph-build time, the Godot resource binds the core Forge `CanActivateAbilityResolver` with the optional target entity resolver and the optional handle resolver.

## Related Docs

- [Resolvers Reference](README.md)
- [Core CanActivateAbilityResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/can-activate-ability-resolver.md)
- [GetAbilityHandleResolver](get-ability-handle-resolver.md)
