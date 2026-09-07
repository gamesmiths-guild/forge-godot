# Interop Nodes

> **Namespace:** `Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action` and `.State`
>
> **Add Node group:** Action → **Interop**, State → **Interop**

These nodes are **Godot-only** — there is no core Forge counterpart, so this page is their reference rather than a Godot-authoring supplement to one.

They are the escape hatches into everything Forge deliberately has no concept of: a scene node's own properties, a game's own scripts, and Godot signals in both directions. All of them are dimension-neutral.

## The Node row

Every node here takes the same optional **Node** input at index 0. Unbound means the node the ability's owner lives on — the nearest spatial ancestor of either kind, falling back to the entity's own node when it belongs to no spatial hierarchy at all. "Act on me" is what an untouched row says.

That lookup is the bridge's `TryGetOwningNode`, which is also what the [`Node From Entity`](../resolvers/scene-graph-resolvers.md#crossing-between-lanes) resolver resolves through, so the two cannot drift.

## The conversion set

One `InteropValueType` dropdown names both value and object lanes for every node here:

`Bool`, `Int`, `Float`, `Vector2`, `Vector3`, `Vector4`, `Plane`, `Quaternion`, `Node` — plus an **Array** checkbox beside it, and a `None` entry where an argument may be absent.

Both settings are declared `AffectsLayout`, so changing either rebuilds the node and the Value row's editor is chosen from the input type the rebuilt node declares.

**Entity is deliberately not in that set.** Writing an entity to a Godot property can only ever mean writing the node it lives on. Spelled as a dropdown entry, that lookup happens silently inside one row; spelled as [`Node From Entity`](../resolvers/scene-graph-resolvers.md#crossing-between-lanes) feeding the row, it is a step the author can see and point at. Reading is the same in reverse through [`Entity From Node`](../resolvers/scene-graph-resolvers.md#crossing-between-lanes).

## Properties

| Node | Arch | Settings | Inputs |
|---|---|---|---|
| `SetNodePropertyNode` | Action | **Property** (text); **Type**; **Array** | 0 Node (optional); 1 Value (required) |
| `NodePropertyOverrideNode` | State | the same three | 0 Node (optional); 1 Value (required) |

`SetNodeProperty` is the direct answer to "Set Variable does not reach the scene nodes": graph variables are two in-memory bags and nothing observes them.

`NodePropertyOverride` captures what it found on activation and restores it on deactivation *or abort*, so a cancelled ability cannot leave a light dimmed or a material swapped for the rest of the run. The Action form is for state meant to outlive the ability — the same pairing [`SetCollisionBits` and `CollisionOverride`](physics-nodes.md#collision-bits) have, for the same reason.

Reading a property back is the [`Node Property`](../resolvers/scene-graph-resolvers.md#reading-a-nodes-own-state) resolver.

**A property path that does not exist is loud.** `SetIndexed` answers an unknown path with nothing at all rather than with a complaint, so a typo would otherwise be a node that runs, reports success and changes nothing. The write checks that the object declares the path's first segment — the segment a typo lands in — before deciding anything is wrong.

## Enabled state

| Node | Arch | Settings | Inputs |
|---|---|---|---|
| `SetNodeEnabledNode` | Action | **Aspect** | 0 Node (optional); 1 Enabled (`bool`, required) |
| `NodeEnabledOverrideNode` | State | **Aspect** | 0 Node (optional); 1 Enabled (`bool`, required) |

**Aspect** is one of `Visible`, `Processing`, `PhysicsProcessing`, `Monitoring`, `Monitorable`.

**Monitoring is an area's switch.** Godot puts `Monitoring` and `Monitorable` on `Area2D` and `Area3D` alone, so pointing one at a body warns and names the distinction: a body's collision *layer and mask* say which layers it occupies and scans, while monitoring says whether an area tracks what is inside it. The warning points at [Set Collision Bits and Collision Override](physics-nodes.md#collision-bits) instead.

Switching monitoring off is now an ordinary state for an area to be in, so [`Area Overlaps`](../resolvers/physics-queries.md#entity-list-queries) and [Overlap](physics-query-nodes.md#overlap)'s existing-area mode report nobody rather than erroring. Worth noting for anything reading an area from C#: a behavior calling `GetOverlappingBodies` directly still errors, because the check belongs to each caller.

## Methods and signals

| Node | Arch | Settings | Inputs | Outputs / Ports |
|---|---|---|---|---|
| `CallMethodNode` | Action | **Method** (text); **Arg 1**; **Arg 2**; **Returns** | 0 Node; 1 Arg 1; 2 Arg 2 | Output 0 `Return` |
| `EmitSignalNode` | Action | **Signal** (text); **Arg 1**; **Arg 2** | 0 Node; 1 Arg 1; 2 Arg 2 | — |
| `SignalListenerNode` | State | **Signal** (text); **One Shot** (deactivates the node the first time the signal fires) | 0 Node | Port 4 `OnSignal` |

**Arguments are filled in order, and the editor enforces it both ways.** The second argument's type is offered only once the first is set, and the second row is hidden unless both are — the runtime stops at the first gap, so a row shown on its own type alone would be one the author filled in and nothing passed. The rows are *required* rather than optional for the same reason: an optional row renders `(None)`, which would read as "no argument" while the runtime passed a zero to keep the positions right.

**Emit Signal checks its argument count and refuses; Call Method does not.** The asymmetry is real rather than an oversight: a signal has one fixed arity to compare against, while a method has optional parameters and varargs. Godot's own error covers the other.

**Signal Listener reports that a signal fired, never what it carried.** Godot matches a connection by arity — a callable taking fewer arguments than the signal emits is an error on every emission, not a silent truncation — so the listener is built with the signal's own argument count, read from `GetSignalList()`. Keeping it a pure trigger is the deliberate half: when the payload is the point, [Overlap](physics-query-nodes.md#overlap) reports the entity it found and Forge's own events carry typed data.

## Debug

| Node | Arch | Settings | Inputs |
|---|---|---|---|
| `DebugNode` | Action | value type; is-array; object type id | 0 Value |

Prints a resolved input of any supported type to the Godot console. It reaches both lanes, which is why it carries the three-setting shape the interop nodes replaced with one enum — it has to switch between the value lane and every registered object variable type. Override `FormatDebugValue` on a [custom object variable type](../variables.md#adding-a-custom-object-variable-type) to control how your own types print.

`DebugNode` lives in the Godot namespace like everything else on this page. It originally declared itself inside the engine-agnostic core's namespace despite shipping with the Godot layer; it was moved outright with no compatibility shim, so a graph resource that referenced the old name is recreated rather than migrated.

## Related Docs

- [Nodes Reference](README.md)
- [Scene Graph and Interop Resolvers](../resolvers/scene-graph-resolvers.md) — `Node Property`, `Node From Entity`, `Entity From Node`, `Node Path`
- [Scene Nodes](scene-nodes.md) — spawning, freeing, reparenting, groups
- [Variables and Data](../variables.md)
