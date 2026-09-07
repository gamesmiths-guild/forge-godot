# Physics Nodes

> **Namespace:** `Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action` and `.State`
>
> **Add Node group:** Action → **Physics**, State → **Physics**

These nodes are **Godot-only** — there is no core Forge counterpart, so this page is their reference rather than a Godot-authoring supplement to one.

They are the *writing* half of the physics set: velocity, impulses, sustained force and collision bits. The reading half — rays, sweeps, overlaps and sight lines — is on [Physics Query Nodes](physics-query-nodes.md).

Every node here is a 2D/3D pair; the tables describe the 3D member, and [what differs in 2D](#what-differs-in-2d) is at the end.

The Action nodes execute once and have no rail. Of the two State nodes, **Force Override reasserts on the fixed step**, because a write applied a different number of times per second on every machine is not the thrust the author wrote; Collision Override does no per-tick work at all — it writes once on activation and undoes it once on deactivation.

## Shared rows

- **Entity (input 0, optional).** Whose body to write to. Unbound means the ability's owner.
- **Node (setting, text).** A path to the body, typically `%Body`. Unlike the [spatial nodes](spatial-nodes.md#node-paths), a path that misses here **falls back to the entity's own node** — see [below](#a-missing-marker-falls-back-to-the-body).

## Velocity and impulses (Action)

| Node | Settings | Inputs | Body types | Behavior |
|---|---|---|---|---|
| `SetVelocity3DNode` | Node | 1 Velocity (`Vector3`, required) | `CharacterBody3D`, `RigidBody3D` | Dash; knockback by aiming Entity at the target instead of the caster. |
| `ApplyImpulse3DNode` | Node | 1 Impulse (`Vector3`, required); 2 At Offset (`Vector3`, optional) | `RigidBody3D` | An offset turns the push into a spin. |
| `SetAngularVelocity3DNode` | Node | 1 Angular Velocity (`Vector3`, required) | `RigidBody3D` | An axis with the rate as its length. Zero stops a spin dead. |
| `ApplyTorqueImpulse3DNode` | Node | 1 Torque (`Vector3`, required) | `RigidBody3D` | The angular Apply Impulse. No offset row: an offset is what turns a push into a spin, and this already is the spin. |

**The angular half is narrower than the linear one, and the engine is why.** Angular velocity and torque exist only on a rigid body — a character body is turned by the game rather than by physics — so pointing an angular node at a `CharacterBody3D` warns and skips the write, naming [Set Rotation 3D and Rotate To 3D](spatial-nodes.md) instead of silently doing nothing.

## Force Override (State)

Holds a body's **constant** force and torque for as long as the node is active, capturing what it found on activation and restoring it on deactivation *or abort*. Thrusters, updrafts, tractor beams, a channelled pull — this is the only thing in the layer that expresses sustained acceleration.

| Setting | Values |
|---|---|
| **Node** | text, placeholder `%Body` |

| Index | Label | Type | Notes |
|---|---|---|---|
| 0 | Entity | `IForgeEntity` | Optional. |
| 1 | Force | `Vector3` | Optional. |
| 2 | Torque | `Vector3` | Optional. |

**Constant force, not repeated `ApplyForce`.** Godot accumulates an applied force per physics frame and clears it, so a node calling `ApplyForce` per update would push harder at 120fps than at 60. Writing the body's constant force hands the integration to the engine at its own rate. The capture-and-restore is what stops a cancelled ability leaving a body accelerating forever.

**Both rows are captured independently**, so binding only one leaves the other's authored value alone. `RigidBody` only, for the same reason as the angular nodes.

Its permanent counterpart needs no node of its own: a constant force is an ordinary property, so [`Set Node Property`](interop-nodes.md#properties) on `constant_force` writes one that outlives the ability.

## Collision bits

The permanent-and-held pair the whole layer uses for writes: the Action form is for state meant to outlive the ability, the State form for state that must not.

| Node | Arch | Settings | Inputs |
|---|---|---|---|
| `SetCollisionBits3DNode` | Action | Field (`Layer`/`Mask`); Operation (`Clear`/`Set`); Node | 1 Bits (`int`, required) |
| `CollisionOverride3DNode` | State | the same three | 1 Bits (`int`, required) |

`CollisionOverride` captures the field on activation and restores it on deactivation *or abort*, so a cancelled dash cannot leave a character permanently intangible.

**It restores only the bits it acted on**, not the whole field. Putting the captured snapshot back would undo everything else that touched the field while it was running — a second override on different bits, or a permanent `Set Collision Bits` — and resurrect the bits those deliberately changed. Two overrides on the *same* bits still resolve to whichever ends last; neither can know what the other found.

Both work on any `CollisionObject`.

## A missing marker falls back to the body

The physics writers opt into a fallback the transform writers refuse: when the **Node** path resolves to nothing, they act on the entity's own node and warn once naming the path. Their `%Body` placeholder is why. Physics state lives on the body and nowhere else, so the path is a *route to the subject* rather than a different subject, and an entity authored without the marker still has a right answer.

A node of the **wrong type** — a `Marker3D` where a body was required — warns separately, because an entity with no marker hits both cases in sequence and one silencing the other would restore exactly the invisibility this exists to fix.

## Seeing the writes

`SetVelocity` and `ApplyImpulse` draw an arrow at the body's position at its true world length, so a velocity arrow reaches where the body gets to in one second and an impulse that is far too strong looks it. `SetAngularVelocity` and `ApplyTorqueImpulse` draw an arrow along the spin axis with the rate as its length; `ForceOverride` holds an arrow for as long as the push lasts, following the body.

All of it is gated on Godot's own **Debug → Visible Collision Shapes** and nothing else. See [Physics Debug Drawing](../physics-debug-drawing.md).

## What differs in 2D

- **A spin is a number.** `SetAngularVelocity2D` takes a `double` in radians per second and `ApplyTorqueImpulse2D` a `double` — a plane has one axis to turn around.
- **The two angular nodes draw nothing**, which is the answer rather than an omission: a 2D spin is about an axis pointing out of the screen, so any arrow drawn in the plane would name a direction the spin does not have.
- Everything else is a mechanical mirror: `SetVelocity2D`, `ApplyImpulse2D`, `ForceOverride2D` (whose Torque row is a `double`), `SetCollisionBits2D` and `CollisionOverride2D`.

## Related Docs

- [Nodes Reference](README.md)
- [Physics Query Nodes](physics-query-nodes.md) — rays, sweeps, overlaps, sight lines
- [Spatial Nodes](spatial-nodes.md) — the non-solving transform writers
- [Spatial Getters](../resolvers/spatial-getters.md) — `Entity Velocity`, `Entity Angular Velocity`, `Character State`, `Character Motion`
- [Physics Debug Drawing](../physics-debug-drawing.md)
