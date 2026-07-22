# AbilityDataResolver

> **Type:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.AbilityDataResolverResource`
>
> **Output Type:** `AbilityData`

Provides an `AbilityData` value from a `ForgeAbilityData` resource, for the ability grant and lookup node inputs (for example the grant nodes and [GetAbilityHandleResolver](get-ability-handle-resolver.md)).

## Authoring in Godot

The editor exposes a single **Ability** picker.

- Assign one `ForgeAbilityData` resource. That resource identifies the ability to grant or look up.

This is a Godot-only authoring resolver: it exists to bridge a `ForgeAbilityData` resource into the runtime `AbilityData` that ability nodes and lookups expect, so it has no separate core-resolver counterpart.

## Runtime Binding

At graph-build time, the Godot resource binds a `ForgeAbilityDataResolver` over the selected `ForgeAbilityData`. The underlying `AbilityData` is materialized lazily on first resolve, so graph building (including editor-time builds) never touches the runtime managers. If no resource is assigned, the resolver pushes an error and binds nothing.

## Related Docs

- [Resolvers Reference](README.md)
- [GetAbilityHandleResolver](get-ability-handle-resolver.md)
