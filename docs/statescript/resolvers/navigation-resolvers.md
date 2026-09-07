# Navigation Resolvers

> **Namespace:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers`
>
> **Runtime:** `Gamesmiths.Forge.Godot.Core.Statescript.Resolvers`

These resolvers are **Godot-only** — there is no core Forge counterpart, so this page is their reference rather than a Godot-authoring supplement to one.

They make navigation *askable* as well as walkable: [`Nav Move To`](../nodes/navigation-nodes.md) walks, and these three answer questions about the walk before it starts. Without them an AI graph cannot branch on reachability, and a blink to the cursor cannot be made to land somewhere legal.

Each is a 2D/3D pair, because `NavigationServer2D` and `NavigationServer3D` are two APIs with different vector types.

| Resolver | Output | Operands |
|---|---|---|
| **Nav Reachable 3D** | `bool` | From; To; **Within** (tolerance) |
| **Nav Path Length 3D** | `double` | From; To |
| **Nav Closest Point 3D** | `Vector3` | **Of** (point) |

## Reachable is not "a path came back"

`NavigationServer.MapGetPath` answers an impossible request with a perfectly good path to the closest point it could reach, so a destination across a chasm returns a full path that simply stops at the edge. The test is whether the path's **last point lands near the one asked for**, which is what the **Within** row is.

The tolerance has to be forgiving, because the destination is snapped onto the navigation mesh before the path is built — even an obviously reachable point rarely comes back exactly. A fresh **Within** row is seeded with `NavigationAgent`'s own `target_desired_distance` — **1.0 in 3D, 10.0 in 2D** — so a check agrees with what an agent walking the same path would report rather than inventing a second standard, and what the editor shows is what runs.

## Path length reports the walk it can make

`Nav Path Length` measures the walk to the **closest reachable point** and does not fail — the honest reading of "how far can I get". That means zero is ambiguous: already there, and no path at all, read the same.

That is the same conflation Godot's own API makes, and the answer is composition — ask `Nav Reachable` first when the difference matters — rather than a second return channel a resolver has no way to express.

## Nav Closest Point

Clamps a point onto walkable floor, which is what makes a ground-targeted blink or a cursor-aimed summon land somewhere legal.

## Related Docs

- [Resolvers Reference](README.md)
- [Navigation Nodes](../nodes/navigation-nodes.md) — `Nav Move To`
- [Spatial Getters](spatial-getters.md) — `Can Fit`, the physics counterpart to a legality check
