# Spatial Getters

> **Namespace:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers`
>
> **Runtime:** `Gamesmiths.Forge.Godot.Core.Statescript.Resolvers`

These resolvers are **Godot-only** — there is no core Forge counterpart, so this page is their reference rather than a Godot-authoring supplement to one.

They read something off the scene node an entity lives on. Every one is a 2D/3D pair, and they share one authoring model, which is why they are documented together: the base carries the two operands they all have, and each resolver adds only its own setting.

## Named for what they read from

A dropdown offering `Position 3D` beside a dozen other sources of a `Vector3` says nothing about where the number comes from. `Entity Position 3D` says it reads a position *off an entity*, which is the part that decides whether it is the resolver you want — and the prefix groups the family together alphabetically in the picker, which is where they are actually chosen from.

## Shared rows

| Row | Meaning |
|---|---|
| **Of** | A nested entity picker. Defaults to the ability's **owner**, and accepts anything that produces an entity — including composed sources like [`Entity At Path`](scene-graph-resolvers.md#crossing-between-lanes), the first result of a query, or a `Conditional`. |
| **Node** | An optional path to a descendant to read instead of the entity's own spatial node. Scene-unique names work, so `%CastPoint` or `%Muzzle` pointed at a marker is how an authored offset is expressed without any code. |

**A missing marker falls back to the entity's own node**, and warns once naming the path. A `%CastPoint` path is an offset from the entity, not a different subject, so an entity authored without the marker still has a right answer: its body. Without the fallback, one graph run against a character with a marker and a plain prop without one would resolve the second to the world origin, and a line of sight aimed at a markerless target would read as a physics bug rather than a missing node.

A `%name` is searched **outward from the graph's entity**, innermost scene first, rather than down from the current scene — see [node paths](../nodes/spatial-nodes.md#node-paths).

## The transform readers

| Resolver | Output | Setting | Notes |
|---|---|---|---|
| **Entity Position 3D** | `Vector3` | **Space** (`Global`/`Local`) | |
| **Entity Direction 3D** | `Vector3` | **Axis** (`Forward`, `Back`, `Right`, `Left`, `Up`, `Down`) | Read off the basis. Godot forward is −Z. |
| **Entity Rotation 3D** | `Quaternion` | **Space** | |
| **Entity Scale 3D** | `Vector3` | **Space** | |
| **Entity Transform Point 3D** | `Vector3` | **Offset** (nested); **Inverse** | Local offset to world point, or back. The generic offset primitive, which is why no cast-point concept enters the engine layer. |

## The physics readers

| Resolver | Output | Setting | Notes |
|---|---|---|---|
| **Entity Velocity 3D** | `Vector3` | — | The velocity the game *asked* for. Compare Character Motion's `RealVelocity`, which is what the slide achieved. |
| **Entity Angular Velocity 3D** | `Vector3` | — | An axis with the rate as its length, so core's `Length` is the spin rate and `Normalize` the axis. `RigidBody` only. |
| **Can Fit 3D** | `bool` | **Destination** (nested, seeded with the entity's own position) | `TestMove` with no motion: would this body fit *there*. |

**Can Fit tests the body's own shapes**, which is why a [Shapecast](physics-queries.md#narrowing-and-testing) is not a substitute — a sweep can only test a shape somebody authored into the graph, and keeping that in sync with the character it stands for goes stale the first time an artist resizes a capsule. It also asks about the destination rather than the path: a blink is *meant* to skip what is in between, and Shapecast is already the resolver for the journey.

It is the guard the non-solving [Move To](../nodes/spatial-nodes.md#move-to) and [Set Position](../nodes/spatial-nodes.md#instant-writers-action) create a need for — a blink into a wall is the classic way an ability breaks a level.

## The character readers

`CharacterBody` exposes its contact state as **methods**, so [`Node Property`](scene-graph-resolvers.md#reading-a-nodes-own-state) cannot read them and [`Call Method`](../nodes/interop-nodes.md#methods-and-signals) is an Action that cannot appear where a condition is wanted. Without these two, a grounded-only ability, an air-only ability and a wall-jump are unauthorable without code. Two resolvers with an enum each cover thirteen methods without thirteen entries in the picker.

| Resolver | Output | Setting |
|---|---|---|
| **Character State 3D** | `bool` | **State**: `OnFloor`, `OnFloorOnly`, `OnWall`, `OnWallOnly`, `OnCeiling`, `OnCeilingOnly` |
| **Character Motion 3D** | `Vector3` | **Value**: `RealVelocity`, `FloorNormal`, `WallNormal`, `LastMotion`, `PositionDelta`, `PlatformVelocity` |

The `Only` variants are not redundant: a character wedged in a corner is on the floor and on a wall at once.

`RealVelocity` is worth calling out — `Entity Velocity` reads the velocity the game *asked* for, which for a character walking into a wall is a full-speed vector into geometry it never moved through. Wall-jump direction is `WallNormal`; impact damage is `RealVelocity`. **The two normals report zero when the contact does not hold**, rather than the stale one Godot keeps.

## What differs in 2D

- **`Entity Rotation 2D` reports a `float` in radians** rather than a quaternion, so core's whole numeric toolbox applies to a facing directly. **`Entity Angular Velocity 2D` reports a `double`** rate rather than an axis with a length.
- **`Entity Direction 2D` offers four axes, not six.** A plane has no up and down of its own, and screen-up is already `Left` or `Right` of a facing, so it takes a `SpatialAxis2D` of its own rather than sharing an enum with two members that would resolve to nothing.
- Everything else — position, scale, velocity, transform point, both character readers, Can Fit — mirrors with the vector type swapped.

## Related Docs

- [Resolvers Reference](README.md)
- [Spatial Nodes](../nodes/spatial-nodes.md) — the writing half of this family
- [Physics Query Resolvers](physics-queries.md)
- [Scene Graph and Interop Resolvers](scene-graph-resolvers.md)
