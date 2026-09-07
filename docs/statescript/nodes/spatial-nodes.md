# Spatial Nodes

> **Namespace:** `Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action` and `.State`
>
> **Add Node group:** Action → **Spatial**, State → **Spatial**

These nodes are **Godot-only** — there is no core Forge counterpart, so this page is their reference rather than a Godot-authoring supplement to one.

They read and write the transform of the node an entity lives on. Every one is a 2D/3D pair; the tables describe the 3D member, and [what differs in 2D](#what-differs-in-2d) is listed at the end.

## Shared rows

Every node here takes the same two, which is what makes the family read consistently:

- **Entity (input 0, optional).** Which entity to move or turn. Unbound means the ability's owner.
- **Node (setting, text).** A path from the entity's spatial node to a descendant to act on instead — `%YawPivot`, `%TargetPoint`, `%Turret`. Scene-unique `%names` are searched outward from the entity's own scene, so a marker inside an instanced level is found; see [Node paths](#node-paths).

The instant writers additionally share a **Space** setting (`Global` or `Local`) except where noted.

## Instant writers (Action)

| Node | Settings | Inputs | Behavior |
|---|---|---|---|
| `SetPosition3DNode` | Space; Node | 1 Position (`Vector3`, required) | Moves the entity instantly. Blink and teleport. |
| `SetRotation3DNode` | Space; Node | 1 Rotation (`Quaternion`, required) | Writes a rotation instantly, preserving scale. |
| `SetScale3DNode` | Node | 1 Scale (`Vector3`, required) | Growing zones. |
| `SetRotationToward3DNode` | **Ignore height** (default on); Node | 1 Target (`Vector3`, required) | Turns to face a point, once. With Ignore height on, the turn stays level rather than pitching at a target's feet. |

`SetRotationToward` faces where the target *was* at the instant it ran. For a facing that keeps following, use [Look At](#look-at).

## Motion over time (State)

All four run on the **fixed step**, not the frame — see [the two update rails](../README.md#in-godot).

### Move To

Interpolates the transform from where the entity is to a destination. **It does not solve collisions**: this is the leap, hook-and-pull and forced-reposition primitive, and it will move a body through a wall. Guard it with the [`Can Fit`](../resolvers/spatial-getters.md#the-physics-readers) resolver, or use [Move Body](#move-body) when the world has to stop it.

| Setting | Values | Meaning |
|---|---|---|
| **Mode** | `Duration`, `Speed` | How the Value row is read. |
| **Easing** | `Linear`, `EaseIn`, `EaseOut`, `EaseInOut` | Applied to progress. |
| **Node** | text | As above. |

| Index | Label | Type | Notes |
|---|---|---|---|
| 0 | Entity | `IForgeEntity` | Optional. |
| 1 | Destination | `Vector3` | Required. Captured at activation. |
| 2 | Value | `double` | Required. Seconds under `Duration`, units per second under `Speed`. |
| 3 | Arc Height | `double` | Optional. Raises the midpoint of the path, which is what makes a leap read as one. |

Port 4 is **OnArrived**, emitted when the move completes rather than when it is aborted. Aborting stops the entity in place. An entity freed mid-move deactivates the node rather than leaving the ability waiting on an arrival that can never come.

### Move Body

The solving counterpart. Sweeps the body toward the destination with `MoveAndCollide` each fixed step and reports what stopped it. The resolved node must be a `PhysicsBody`, and it warns and skips the move when it is not.

`MoveAndCollide` rather than `MoveAndSlide` is not an implementation detail: `MoveAndSlide` is the game's own character-controller entry point, is `CharacterBody`-only, and reads and writes the body's velocity while applying floor snapping and slope detection. An ability calling it would move the body a second time each step and fight the controller for ownership of that velocity — the exact layering mistake [Nav Move To](navigation-nodes.md) avoids by writing a velocity and letting the game move.

| Setting | Values | Meaning |
|---|---|---|
| **Mode** | `Duration`, `Speed` | As Move To. |
| **When Blocked** | `Stop`, `Slide` | Whether a refused step ends the move or slides along the surface. |
| **Node** | text | As above. |

| Index | Label | Type | Notes |
|---|---|---|---|
| 0 | Entity | `IForgeEntity` | Optional. |
| 1 | Destination | `Vector3` | Required. |
| 2 | Value | `double` | Required. |

| Kind | Index | Name | Notes |
|---|---|---|---|
| Output | 0 | Blocker Entity | The entity the move met, when it has one. |
| Output | 1 | Blocker Node | The collider itself. |
| Port | 4 | OnArrived | Reached the destination. |
| Port | 5 | OnBlocked | Stopped short, or ran out of time under `Slide`. |

**Both responses are time-bounded** by the duration the move would have taken unobstructed, which is what keeps `Slide` honest — a slide grinding along a wall forever is not something a graph can author by accident. Running out of time reports `OnBlocked`, because not arriving is what being blocked means; under `Slide` that is the only way it fires.

There is no easing setting, and that is not an omission: distributing travel over time describes a path the body is no longer on the moment a collision displaces it.

### Rotate To

Turns to a rotation captured at activation, over a duration or at a rate.

| Setting | Values |
|---|---|
| **Mode** | `Duration`, `Speed` — the same `MoveToMode` Move To uses |
| **Node** | text |

Inputs are Entity (0, optional), **Rotation** (1, `Quaternion`, required) and **Value** (2, `double`, required). Port 4 is **OnAligned**. A zero quaternion — what an unfilled operand resolves to — is rejected rather than turned to.

### Look At

The turn that keeps turning. The target is re-resolved every fixed step, so a bound [`Entity Position 3D`](../resolvers/spatial-getters.md) follows whoever is moving: a turret holding a lead, a beam that tracks, a boss keeping the player in front of it.

| Setting | Values | Meaning |
|---|---|---|
| **Ignore height** | checkbox, default on | Keeps the turn level. |
| **Node** | text | Typically `%YawPivot`. |

| Index | Label | Type | Notes |
|---|---|---|---|
| 0 | Entity | `IForgeEntity` | Optional. |
| 1 | Target | `Vector3` | Required, re-read every step. |
| 2 | Speed | `double` | Optional ceiling in radians per second. Unbound snaps. |

There is no aligned port: [`Is In Cone 3D`](../resolvers/physics-queries.md#narrowing-and-testing) over the caster's forward is the layer's aim test. The lag a speed ceiling creates is the point — it is what makes a tracking attack dodgeable, which is why the rate is a maximum rather than a rate to be met.

## Node paths

A `%name` in the **Node** setting is searched **outward from the graph's entity**, innermost scene first, rather than down from the current scene. A game whose menu instantiates levels as children leaves `CurrentScene` pointing at the outer shell, which knows nothing about the names inside the level. Ordinary relative paths keep the current scene as their root; only `%names` get the walk, because only they have the registration problem.

**A missing marker does not fall back here.** Every node on this page is pointed at a node that *is* the subject — substituting the entity's body would turn "rotate the turret" into "rotate the tank" — so a path that misses warns and skips the write. The physics writers are the opposite case; see [Physics Nodes](physics-nodes.md#a-missing-marker-falls-back-to-the-body).

## What differs in 2D

- **Rotation is a number.** `SetRotation2D` and `RotateTo2D` take a `double` in radians. Core's whole numeric toolbox — lerp, wrap, delta angle, deg-to-rad — applies to a facing directly, with no quaternion resolvers in between.
- **`SetRotationToward2D` and `LookAt2D` have no Ignore height setting.** There is no pitch to suppress in a plane, so the 2D nodes are the flattened ones by construction.
- **`RotateTo2D` resolves a signed delta once at activation** rather than interpolating two absolute angles, because a plane's rotation keeps counting past a full turn and lerping the raw numbers would go the long way round. It has no zero guard either, since zero radians is a facing a graph can genuinely mean.
- **`MoveTo2D`'s arc subtracts**, because screen up is −Y.

## Related Docs

- [Nodes Reference](README.md)
- [Spatial Getters](../resolvers/spatial-getters.md) — the reading half of this family
- [Physics Nodes](physics-nodes.md) — velocity, impulses and collision bits
- [Navigation Nodes](navigation-nodes.md) — `Nav Move To`, the pathfinding walk
