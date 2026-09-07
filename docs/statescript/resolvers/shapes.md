# Shape Resolvers

> **Namespace:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers`
>
> **Output Types:** `Godot.Shape3D` (object variable type `Shape3D`) and `Godot.Shape2D` (`Shape2D`)

These resolvers are **Godot-only** — there is no core Forge counterpart, so this page is their reference rather than a Godot-authoring supplement to one.

They build the shape a physics query sweeps. Every dimension is a nested resolver, so a radius can scale with an attribute, a level or a variable.

## A shape is a value

The alternative was an enum setting plus fixed numeric inputs, or an exported `Shape3D` — and a radius that cannot scale with an attribute is not much use. Building shapes the way scenes and nodes are built, as an object variable type plus a resolver family, gives both: a shape can live in a variable, be built once on entry and swept by every tick of a loop, or be chosen by a `Conditional` resolver.

**Each shape names its own dimensions**, which one fixed set of inputs could not: a capsule takes a radius and a *height*, not an extents vector.

**Placement belongs to the query, not the shape.** A `Shape3D` has no transform of its own, so the [Overlap](../nodes/physics-query-nodes.md#overlap) and [Shapecast](../nodes/physics-query-nodes.md#shapecast) rows carry Position and Rotation — which is what lets a box or a capsule be turned.

## 3D shapes

| Picker entry | Operands | Notes |
|---|---|---|
| **Sphere** | Radius | |
| **Box** | Size | Full size, matching Godot's `BoxShape3D.Size`. |
| **Capsule** | Radius; Height | Total height, caps included, clamped to at least twice the radius. |
| **Cylinder** | Radius; Height | Total height. |
| **Cone** | Angle (deg); Range | A convex hull — see [below](#the-cone-and-the-wedge). |
| **Constant** | an exported `Shape3D` | For a convex hull, a height map, or a primitive whose size never changes. |

## 2D shapes

| Picker entry | Operands | Notes |
|---|---|---|
| **Circle** | Radius | The 2D answer to both the sphere and the cylinder. |
| **Rectangle** | Size | Full size, matching Godot's `RectangleShape2D.Size`. |
| **Capsule** | Radius; Height | As the 3D capsule. |
| **Wedge** | Angle (deg); Range | A plane's cone, and a genuinely simpler shape. |
| **Constant** | an exported `Shape2D` | For a convex polygon, or a primitive whose size never changes. |

## The cone and the wedge

Godot has no cone collision shape, so `Cone` builds the convex hull of an apex and a ring on the rim. That *is* a `Shape3D` and goes anywhere a shape goes — swept by a Shapecast, held by an Overlap node.

**It does not agree with [`Entities In Cone`](physics-queries.md#the-cone-is-a-sphere-plus-a-filter) at the edges.** The hull's facets are chords of the rim circle, so they sit **inside** the true cone, and a target near the rim between two facets is found by the analytic query and missed by the hull. Both ship, because "cone" turns out to be two different requests: *test* this cone, and *sweep* this cone.

**Both point where a character faces**, not where Godot's primitives point. Godot's cylinder and capsule are Y-up; a gameplay cone is aimed along a facing, so the hulls are built along −Z in 3D and +X in 2D. Binding a query's Rotation to [`Entity Rotation 3D`](spatial-getters.md) aims one correctly with nothing composed in between, and the apex sits at the shape's own origin so binding Position to the caster puts the point of the cone on the caster.

The aperture is clamped just below a half turn in both, because a hull cannot express a reflex cone: past that the rim runs away and the hull closes around behind its own apex. The wedge needs only its two edges and an arc, which is the rare case where the 2D twin is genuinely simpler rather than merely flatter.

## What is deliberately absent

**Every Godot shape with numbers worth authoring has a resolver.** What is left is reached through **Constant** and has nothing to parameterise: the polygon and heightmap shapes are authored geometry rather than dimensions, `SeparationRayShape` is a character-controller foot ray, and `WorldBoundaryShape` is an infinite half-space that would match half the world. `SegmentShape2D` is parametric but is a raycast by another name, and has no 3D twin to pair with.

**The notable shape that is not expressible is the ring.** A donut area of effect is not convex, so no hull can describe it: `Overlap` of the outer radius with core's `Except` over the inner one is how that is composed.

## Two dimensions, two types

Shapes are the one place the pairs do not line up name for name — 2D has no cylinder, and its circle is what both the sphere and the cylinder are used for.

`Shape2D` is a **separate object variable type** from `Shape3D` rather than one shared "shape", because the two physics servers do not mix. Handing a 2D query a 3D shape is not a narrower answer, it is no answer at all, and a dropdown that can express the mistake is a dropdown that will.

## Related Docs

- [Resolvers Reference](README.md)
- [Physics Query Resolvers](physics-queries.md)
- [Physics Query Nodes](../nodes/physics-query-nodes.md)
- [Variables and Data](../variables.md) — object variable types
