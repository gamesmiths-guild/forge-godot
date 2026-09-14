# LookAtResolver

> **Type:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.LookAtResolverResource`
>
> **Output Type:** `Quaternion`

Authors a rotation that looks from one position at another, for any quaternion input — an instantiation rotation, `Set Rotation 3D`, the target of a `Rotate To 3D`.

## Authoring in Godot

- Three nested `Vector3` operands: **From**, **To** and **Up**. `Entity Position 3D` for From and a target's position for To is the usual pair; Up is almost always the constant `(0, 1, 0)`.
- Typically bound to [`InstantiateScene3DNode`](../nodes/scene-nodes.md#the-instantiating-pair)'s Rotation to launch a [`ForgeProjectile3D`](../../nodes.md#forgeprojectile2d--forgeprojectile3d) at something, which is the whole aiming story for a projectile: it flies along its own −Z.
- For a facing that keeps following a target, use the [Look At](../nodes/spatial-nodes.md#look-at) State node instead; this resolves the rotation once, wherever it is read.

## It faces −Z

Core's `LookAtResolver` builds its rotation with **+Z as forward**, which is as good a convention as any for a library with no engine behind it. Godot's forward is **−Z**, and everything in this layer that reads or writes a facing follows the engine: `Entity Direction 3D`'s `Forward`, `Set Rotation Toward 3D`, `Look At 3D`, `Camera Forward 3D`, a `ForgeProjectile3D` in flight. Written to a node as-is, core's answer faces exactly away from the target — a projectile spawned with it flies backwards.

So the Godot resource does not bind core's resolver. It binds `GodotLookAtResolver`, which builds the rotation with `Basis.LookingAt`, the same call `Look At 3D` and `Set Rotation Toward 3D` make. The quaternion it produces can be written straight to a node, and it agrees with what `Entity Rotation 3D` reads back off one.

Degenerate inputs resolve the way core's do: a zero-length line of sight gives identity, an up vector along the line of sight falls back to world up, and a vertical line of sight falls back to world right.

## Runtime Binding

At graph-build time the resource builds its three operands through the standard nested-resolver factories and hands them to `GodotLookAtResolver`. Saved graphs are unaffected: the resolver type id is still `LookAt`, and the operands are unchanged.

## Related Docs

- [Resolvers Reference](README.md)
- [Spatial Getters](spatial-getters.md) — `Entity Rotation 3D` and `Entity Direction 3D`, the readers this agrees with
- [Spatial Nodes](../nodes/spatial-nodes.md#look-at) — the Look At and Set Rotation Toward nodes
- [Core LookAtResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/lookat-resolver.md)
