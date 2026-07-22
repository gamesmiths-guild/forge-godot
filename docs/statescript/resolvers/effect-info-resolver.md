# EffectInfoResolver

> **Type:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.EffectInfoResolverResource`
>
> **Output Type:** `int`

Aggregates stack, instance, and level information over the active applications of an effect on an entity — the "how many stacks do I have?" query — for node inputs that accept an `int`.

## Authoring in Godot

The editor exposes an **Effect** picker, an **Info** dropdown, and an **Entity** selector.

- **Effect**: assign the `ForgeEffectData` resource to query for.
- **Info**: selects which aggregate to compute — `TotalStackCount`, `InstanceCount`, or `MaxLevel`.
- **Entity**: selects which entity to inspect — `Owner`, `Source`, `Target`, a `Variable`, or the iterated `Element` (available only inside an array operation's per-element operand). Defaults to the ability owner.

## Runtime Binding

At graph-build time, the Godot resource binds a lazy resolver wrapping the core Forge `EffectInfoResolver`. The `ForgeEffectData` is converted to runtime `EffectData` only on first resolve, so graph building (including editor-time builds) never touches the runtime managers. If no effect data is assigned, the resolver pushes an error and falls back to a default `int`.

## Related Docs

- [Resolvers Reference](README.md)
- [Core EffectInfoResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/effect-info-resolver.md)
