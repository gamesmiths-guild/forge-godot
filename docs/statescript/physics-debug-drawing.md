# Physics Debug Drawing

> **Runtime Types:** `Gamesmiths.Forge.Godot.Core.Statescript.Physics.PhysicsDebugDraw3D` / `PhysicsDebugDraw2D`

An overlap shape, a ray and an impulse describe geometry that exists only for the instant it is asked about, which is exactly the geometry a scene view cannot show. Forge for Godot draws it.

## There is nothing to author

Drawing is gated entirely on Godot's own **Debug → Visible Collision Shapes** — the same switch that reveals the shapes that *are* in the scene. There is nothing to turn on per node, nothing to remember to turn off, and no second concept to learn.

With the switch off every entry point returns on one flag read and allocates nothing. In the editor's own tree the flag is always off, so authoring draws nothing.

## What gets drawn

| Item | Drawn |
|---|---|
| `Overlap`, `Line Of Sight`, `Raycast` (resolvers and one-shot nodes) | Flashed for a third of a second where the query ran, coloured by the answer, so a blocked line, a missed ray or an overlap that caught nobody is told apart without reading a variable. |
| `Entities In Cone` | The cone itself, not the sphere behind it. Godot has no cone collision shape and so no debug mesh to borrow, so the wireframe is built by hand: four edges to a rim that sits on the sphere the query actually swept. |
| `Shapecast` (resolver and node), `Sweep` | The shape at its resting transform **and a line down the middle of the sweep at its full reach**. The outline alone says where the sweep stopped and nothing about where it came from; on a hit, the gap between the end of the line and the shape is the distance the sweep was stopped short by. This is what Godot's own `ShapeCast` gizmo draws, for the same reason. |
| `Closest Entity` | The one it picked, outlined. It runs no query, so it draws no query geometry — but *which* of the group it chose is the one thing the query that found the group cannot show. |
| `Entities At Point` | Short axis lines crossing on the point, coloured by the answer, **and the outline of every entity found**. A cross rather than a small sphere: the query has no radius, and anything with a volume would be read as its reach. |
| `Can Fit` | A line from where the body is to where it was asked to fit, **and the body's own collision shapes drawn at that destination** — green when it fits, red when it does not. Every owner the body has is drawn and disabled ones are left out, so what appears is the geometry the answer was computed from. On a failure, the outline sitting inside whatever blocked it *is* the answer. |
| `Force Override` | An arrow at the body's own position, held for as long as the push lasts and following it, so a thrust that is far too strong looks it. |
| `Set Velocity`, `Apply Impulse` | An arrow at its true world length, so a velocity arrow reaches where the body gets to in one second. |
| `Set Angular Velocity`, `Apply Torque Impulse` | An arrow along the spin axis with the rate as its length — the same reading `Entity Angular Velocity` reports. **Their 2D twins draw nothing**, and that is the answer rather than an omission: a 2D spin is a scalar about an axis pointing out of the screen, so any arrow drawn in the plane would name a direction the spin does not have. |
| `Overlap` (State, transient), `Ray`, `Line Of Sight` (State) | Held for as long as the node is active, updated every poll, and recoloured by the answer: an armed trap reads as armed and turns as something steps into it, a beam as it acquires and loses. |
| `Area Overlaps`, `Overlap` (State, existing area) | No shape. The area is in the scene and Godot already draws it; a second wireframe on top of the engine's is noise. The entities inside it are outlined, which the engine's wireframe does not say. |
| `Is In Cone` | Nothing. It tests one point against numbers and touches no physics server. |

## Outlining what a query found

**A query's own geometry says where the question was asked; only an outline on the answer says who answered it.** A cleave drawn over a crowd shows a cone and leaves the author counting who was inside it by hand; a ray drawn through two overlapping characters says nothing about which one it reported.

So every query that produces entities outlines them, in the query's own colour. **A one-shot query outlines on every run; a monitored one outlines on the transition** — the same rule the geometry follows, because an outline redrawn every poll would stack a fresh flash on the last one until the highlight was a permanently lit body. `Overlap` outlines each entity as it enters, `Ray` and `Sweep` outline what they just acquired, and `Line Of Sight` outlines whatever just blocked it.

### The project setting

**Project → Project Settings → General**, under **Forge → Statescript → Highlight Query Targets**. It is a basic setting, so no Advanced Settings toggle is needed to see it.

`forge/statescript/highlight_query_targets`, **default on**, and consulted only when Visible Collision Shapes is already on. Enabling the plugin declares it, so it is there to switch off from the first run.

It does not appear in `project.godot` until you actually change it: Godot skips saving a property whose value still equals its initial value, which is what keeps an untouched project's file clean.

Per-query checkboxes were the alternative and would have been twenty settings answering one question, against the rule that debug drawing is never authored. The answer is the same for every query in a project and it is a developer preference, which is exactly what a project setting is for — and the reason to have a switch at all is that a crowded scene is where the outlines are most useful *and* most overwhelming, which is a judgement no default can make. With it off, the query geometry and its colour still say whether anything was found.

## How it is drawn

Line geometry is one shared `ImmediateMesh` per marker and shapes reuse `Shape3D.GetDebugMesh()` — the same mesh Godot draws for a collision shape — so a monitored query stays allocation-free once it is running.

Markers are parented to the **owner's own viewport** rather than to the main scene, so a game rendering its world inside a sub-viewport draws its debug geometry in that world instead of an empty one.

**2D needs a script**, because a canvas item has no counterpart to a mesh instance holding geometry: everything a `CanvasItem` shows comes out of its own `_Draw`. `PhysicsDebugMarker2D` is that script. It holds either a shape and its transform or a run of segments, and redraws whichever it was last given; shapes reach `Shape2D.Draw`, the same call Godot makes for a collision shape in the scene, so both dimensions borrow the engine's own drawing rather than reimplementing it.

Two 2D details are not transcriptions of the 3D side: the container node is named apart from the 3D one, because both dimensions can be alive in one viewport and a container found by name would be the wrong kind of node for whichever created it second; and the arrow head is capped in **pixels** rather than metres, since that is what a 2D world measures distance in.

## Related Docs

- [Statescript](README.md)
- [Physics Query Nodes](nodes/physics-query-nodes.md)
- [Physics Nodes](nodes/physics-nodes.md)
- [Physics Query Resolvers](resolvers/physics-queries.md)
