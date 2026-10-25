# Camera and Aim Resolvers

> **Namespace:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers`
>
> **Runtime:** `Gamesmiths.Forge.Godot.Core.Statescript.Resolvers`

These resolvers are **Godot-only** — there is no core Forge counterpart, so this page is their reference rather than a Godot-authoring supplement to one.

They answer "where is the player looking, and where is the player pointing".

| Resolver | Output | Settings and operands |
|---|---|---|
| **Camera Position 3D** | `Vector3` | — |
| **Camera Position 2D** | `Vector2` | — |
| **Camera Forward 3D** *(unpaired)* | `Vector3` | Input Source |
| **Mouse World Position 3D** | `Vector3` | Input Source; **Mode** (`PhysicsRay`/`PlaneIntersect`); Mask; Max Dist |
| **Mouse World Position 2D** | `Vector2` | Input Source |

`Camera Forward 3D` is shooter centre-aim, and has no 2D twin because a 2D camera has no forward.

## No entity decides whose camera

A camera is not something an entity *has* — it is how the graph's own player sees, which is a property of the ability being run. So "whose camera" is answered the same way "which physics world" already is: **the viewport the ability's owner is standing in**. Split screen therefore works without authoring anything, since each player's graph reads its own half.

That is also why they are not prefixed `Entity`, unlike the [spatial getters](spatial-getters.md#named-for-what-they-read-from).

## Mouse World Position 3D

| Mode | Behavior |
|---|---|
| `PhysicsRay` | Casts from the cursor into the world and reports what it meets. **Mask** and **Max Dist** apply. |
| `PlaneIntersect` | Intersects the cursor ray with the horizontal plane through the caster. Nothing is queried. |

**The mask row is hidden in Plane Intersect mode** rather than left fillable and ignored — the same rule the node editors apply through `IsSettingVisible`. The hidden row keeps its value for when the mode comes back.

**Max Dist starts on a constant rather than at zero.** A nested operand has no unbound state, so an untouched distance would be zero and every query would resolve onto the camera itself; the row is seeded with the 1000 units [`AimActivationData`](../nodes/custom-nodes.md#built-in-activation-data-providers) already uses.

## Mouse World Position 2D takes no settings at all

**And it is not a camera resolver**, which is the part worth writing down. A 2D cursor already names a point on the plane the game is played on, so undoing the viewport's canvas transform *is* the answer: there is no mode to pick between, no ray to mask, and no reach to limit.

It reads through the owner's own node rather than through a camera, because a 2D game without a `Camera2D` still has a viewport with a canvas transform, and a resolver that demanded a camera would report nothing for the games that never add one. `Camera Position 2D` does require one, because the centre of the view is genuinely a thing only a camera has.

## Input Source

**Mouse World Position 3D**, **Mouse World Position 2D** and **Camera Forward 3D** take an **Input Source** entity, the ability's owner by default. When that entity has an `Input`, they resolve its aim for the current physics step instead of reading the device, as the core [InputAimResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/input-aim-resolver.md) does: the point aimed at, or the direction. The aim was found by the entity's [`ForgeInputCollector`](../../nodes.md#forgeinputcollector), so Mode, Mask and Max Dist only apply to device reads. An input without aim, from a collector with no aim source, resolves to zero and warns once.

The two camera position resolvers have no Input Source: a camera is how the local player sees, not something the input carries.

## Aim as a payload

Sampling aim *once*, at activation, is usually better than polling — it is required for authoritative or networked games, and it is what lets a sub-ability be activated with the aim the parent resolved. [`AimActivationData` and `AimActivationData2D`](../nodes/custom-nodes.md#built-in-activation-data-providers) are the standard payload for that, with `FromCamera`/`FromMouseGround` in 3D and `FromMouse`/`FromFacing` in 2D.

## Related Docs

- [Resolvers Reference](README.md)
- [Input Resolvers](input-resolvers.md)
- [InputActionNode](../nodes/input-action-node.md)
- [Custom Nodes](../nodes/custom-nodes.md#built-in-activation-data-providers) — the aim payloads and their providers
