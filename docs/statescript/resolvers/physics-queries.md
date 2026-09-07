# Physics Query Resolvers

> **Namespace:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers`
>
> **Runtime:** `Gamesmiths.Forge.Godot.Core.Statescript.Resolvers`

These resolvers are **Godot-only** — there is no core Forge counterpart, so this page is their reference rather than a Godot-authoring supplement to one.

They ask the physics world a question and hand back a value, so they compose: an `Entity[]` feeds `ForEach`, `Where`, `OrderBy` and `Except`, and a `bool` works inside a `Where` lambda. Queries whose answer is several values at once — a hit position *and* a normal *and* a distance — are [nodes](../nodes/physics-query-nodes.md) instead, so one query writes them all atomically.

Every one is a 2D/3D pair.

## Shared operands

- **Mask** (`int`, nested). **Zero means every layer.** A mask of zero can never find anything, so it is never a useful authored value, and reading it literally would make an untouched row silently disable the query.
- **Ignore** (`Entity[]`, nested, seeded with the ability's owner). What the query keeps off. It is a *list* because both ends of a query sit inside a body: a ray from a character's own position starts at its feet, outside its own capsule by a hair, and a line drawn to a marker on a target ends inside the target. Being a list also means an overlap's results can be fed straight in, for a check that should pass through a whole group.
- **Include Areas** (checkbox) where the query can meet one.

**A nested operand has no unbound state**, so every operand whose sensible default is not zero is *seeded* with the resolver that expresses it — Overlap's Position starts on `Entity Position 3D`, its Shape on a Sphere, both ends of Line Of Sight on `Entity Position 3D`, Shapecast's Origin and Direction on the owner's position and forward. An untouched row runs what the editor shows.

## Entity-list queries

| Resolver | Output | Operands | Notes |
|---|---|---|---|
| **Area Overlaps 3D** | `Entity[]` | Of (entity); **Area** (path); Ignore | Reads an existing `Area3D`'s overlaps. The entity plus the path names *whose* area. |
| **Overlap 3D** | `Entity[]` | Shape; Position; Rotation; Mask; Ignore | A transient query. **No entity operand** — Position says where, Ignore says who is left out. |
| **Entities In Cone 3D** | `Entity[]` | Origin; Direction; Range; **Angle (deg)**; Mask; Ignore | Earns a slot because the composed form is five or more resolvers deep. |
| **Entities At Point 3D** | `Entity[]` | Position; Mask; Ignore | `IntersectPoint` — the one query with no shape behind it. |

**`Entities At Point` is not an overlap with a small sphere.** A sphere needs a radius, and that radius is the whole difference between "standing exactly here" and "standing near here". What occupies a tile, whether a spawn point is free; in 2D, the picking query.

## Narrowing and testing

| Resolver | Output | Operands | Notes |
|---|---|---|---|
| **Closest Entity 3D** | `Entity` | **Entities**; **Position** (seeded with the owner's position) | Runs no query of its own — it narrows a group something else found. |
| **Is In Cone 3D** | `bool` | Point; Origin; Direction; **Angle (deg)**; Range | The angle test on its own, so it composes in a `Where` over any entity array. Range is optional: zero means unlimited. |
| **Line Of Sight 3D** | `bool` | From; To; Ignore; Mask | Between two **points**, not two entities. |
| **Shapecast 3D** | `Entity` | Shape; Origin; Direction; Max Dist; Rotation; Mask; Ignore | Swept cast, first hit: a ray with the width of the thing being checked. |

**`Closest Entity` is the chain-lightning primitive**: overlap, then closest, then core's `Except` over what has already been struck, repeated. Running no query is exactly what makes it compose.

**Line Of Sight takes points rather than entities** because an entity's position is an [`Entity Position 3D`](spatial-getters.md) away, and points also cover a sight check to where the player clicked or to a predicted intercept, which have no entity to name. Both ends are seeded with `Entity Position 3D`, and its Ignore row with the owner **and** the target, since both ends of a line usually sit inside a body.

**`Is In Cone` and `Entities In Cone` share one test.** The angle check was originally locked inside the query, which broke the layer's own rule that a shortcut ships *beside* its parts: a cone filter could not be applied to an area's overlaps, a child list or a variable. Sharing the test also means a filter can never disagree with the query it mirrors.

## The cone is a sphere plus a filter

Godot has no cone collision shape and no physics-server call that takes one, so `Entities In Cone` sweeps the sphere the aperture is inscribed in and tests each result by angle. Two things follow: **the range is the cone's slant reach** rather than its depth, because that is what a sphere radius is; and the cone exists only for the instant it is tested, which is why it cannot be swept by a Shapecast or held by an [Overlap node](../nodes/physics-query-nodes.md#overlap).

When you need a cone that *is* a shape, [`Cone`](shapes.md#the-cone-and-the-wedge) builds a convex hull. The two do not agree at the edges, and the difference is documented there.

## An aperture is in degrees

**It is the only angle in the layer that is.** Every other angle here is a rotation — it gets lerped, wrapped, read off a transform or handed to a quaternion, and core's numeric toolbox speaks radians throughout. An aperture is none of those: it is a design figure typed once and never computed, and radians would put a `DegToRad` in front of every cone in the game to say what `90` already says. The row is labelled `Angle (deg)` so the exception is visible where it is authored.

**The authored figure is the whole aperture** and is halved internally, because a 90-degree cleave means 45 degrees either side of the facing everywhere that phrase is used.

## What a resolver cannot return

`Shapecast` computes a hit position, a normal, a collider and a distance and can hand back only one of them — that is what a resolver *is*. When the rest matter, use the [`Shapecast` Condition node and the `Sweep` State node](../nodes/physics-query-nodes.md), which write all five outputs in the same order and with the same names the ray nodes use.

## What differs in 2D

The whole family mirrors, with `Overlap 2D`'s and `Shapecast 2D`'s rotation operands being angles in radians rather than quaternions, and the cone being a wedge. Line Of Sight, Area Overlaps, Closest Entity, Is In Cone and Entities At Point differ only in vector type.

## Related Docs

- [Resolvers Reference](README.md)
- [Physics Query Nodes](../nodes/physics-query-nodes.md) — the compound-result and monitored forms
- [Shape Resolvers](shapes.md)
- [Spatial Getters](spatial-getters.md) — `Can Fit`, and what feeds a Position operand
- [Physics Debug Drawing](../physics-debug-drawing.md)
