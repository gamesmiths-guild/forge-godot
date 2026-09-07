# CollisionMaskResolver

> **Type:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.CollisionMaskResolverResource`
> **Output Type:** `int`

Authors a collision layer or mask as the bit grid Godot's own inspector uses, instead of as a number.

A layer field is a set of thirty-two bits, so `12` means the third and fourth layers rather than the twelfth. This resolver removes that translation: the layers are picked as squares and named by the project, and what reaches the graph is the resulting bit field as a plain `int` — identical to a **Constant** holding the same number.

## Authoring in Godot

The grid is a port of the engine's own, so it behaves the way the one on a `CollisionObject2D` does:

- **Click** a square to toggle that layer; **drag** across squares to paint the same change over a run of them.
- **Ctrl/Cmd + click** a square for solo mode — that layer alone. Clicking the same square again inverts the selection, which reads as "everything but this one".
- **Hover** a square for its name and its `Bit N, value X` line.
- The blocks that do not fit on one row are hidden behind the **arrow** at the end; clicking it unfolds the rest. Whether it is unfolded is view state, so it does not enter the undo history.
- The **⋮ button** lists the named layers as check items, which is how a mask is read and set without counting squares. Layers with no name are left out; when none are named it says so.

Layer names come from **Project Settings → General → Layer Names → 2D Physics / 3D Physics**, the same settings the inspector reads. Which of the two sets is shown is decided by the slot the resolver sits in — a 2D query shows 2D names — so there is nothing to pick and nothing to get wrong. A layer that has not been named shows as its number.

## Where it is the default

Every slot a node declares as a layer field starts on this resolver rather than on a plain constant. That covers the mask operands of the [physics query resolvers](physics-queries.md), the Mask rows of the [physics query nodes](../nodes/physics-query-nodes.md), and the Bits rows of `Set Collision Bits` and `Collision Override` ([physics nodes](../nodes/physics-nodes.md)).

**The optional Mask rows are seeded too**, which is the one place a fresh optional input does not rest on `(None)`. It is safe there and nowhere else: an unbound mask reads as zero, and a mask of zero already means every layer, so the two states are the same query. Showing the grid from the start therefore says what the row does without deciding anything.

The seeding happens once, when the node is created. Choosing `(None)` afterwards genuinely clears the slot and it stays cleared — reopening the graph will not put the grid back.

It is offered on any `int` input as well, so a mask can be authored once into a graph variable and bound to several queries. A slot with no world of its own keeps the names of the world it was authored in.

## Zero means every layer — on a query

A query mask of zero can never find anything, so the query resolvers and nodes read zero as **every layer** rather than as none. An untouched grid is therefore an unfiltered query, not a dead one.

That rule belongs to the queries, not to the grid. On `Set Collision Bits` and `Collision Override`, whose Bits row is a set of bits to write rather than a filter, zero means exactly no bits.

## Runtime Binding

At graph-build time this resource binds a core Forge `VariantResolver` holding the bit field as an `int`. The layer space and the unfolded state are authoring metadata and reach nothing at runtime.

## Related Docs

- [Resolvers Reference](README.md)
- [Physics Query Resolvers](physics-queries.md)
- [Physics Query Nodes](../nodes/physics-query-nodes.md)
- [Physics Nodes](../nodes/physics-nodes.md)
