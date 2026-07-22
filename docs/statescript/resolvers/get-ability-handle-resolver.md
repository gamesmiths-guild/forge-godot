# GetAbilityHandleResolver

> **Type:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.GetAbilityHandleResolverResource`
>
> **Output Type:** `AbilityHandle`

Looks up the `AbilityHandle` of a granted ability on an entity — the entry point for cross-ability queries such as "the cooldown of my other ability". Feed its result into the `Ability` input of resolvers like [AbilityCooldownResolver](ability-cooldown-resolver.md), [AbilityStateResolver](ability-state-resolver.md), or [CanActivateAbilityResolver](can-activate-ability-resolver.md).

## Authoring in Godot

The editor exposes an **Ability** picker, an **Entity** selector, and an **Exact source match** checkbox.

- **Ability**: assign the `ForgeAbilityData` resource identifying the granted ability to look up.
- **Entity**: selects which entity to inspect — `Owner`, `Source`, `Target`, a `Variable`, or the iterated `Element` (available only inside an array operation's per-element operand). Defaults to the ability owner.
- **Exact source match**: when unset, the lookup matches the ability regardless of its granting source. When enabled, only the instance whose granting source is exactly the resolved source matches — including `null`, which then finds only abilities granted without a source.

## Runtime Binding

At graph-build time, the Godot resource binds a lazy object resolver wrapping the core Forge `GetAbilityHandleResolver`. The `ForgeAbilityData` is converted to runtime `AbilityData` only on first resolve, so graph building (including editor-time builds) never touches the runtime managers. If no ability data is assigned, the resolver pushes an error and binds nothing.

## Related Docs

- [Resolvers Reference](README.md)
- [Core GetAbilityHandleResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/get-ability-handle-resolver.md)
- [AbilityCooldownResolver](ability-cooldown-resolver.md)
- [AbilityStateResolver](ability-state-resolver.md)
- [CanActivateAbilityResolver](can-activate-ability-resolver.md)
