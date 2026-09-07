# Scene Nodes

> **Namespace:** `Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action` and `.State`
>
> **Add Node group:** Action → **Scene**, State → **Scene**

These nodes are **Godot-only** — there is no core Forge counterpart, so this page is their reference rather than a Godot-authoring supplement to one.

They cover the two things a graph does to the scene tree itself: putting scenes into it, and moving or removing nodes already in it. Godot groups round it off, since a group is how a level names a set of nodes that no path or hierarchy walk can reach.

## The instantiating pair

`InstantiateScene3D`/`InstantiateScene2D` (Action) spawn and forget. `Scene3D`/`Scene2D` (State) own what they spawned: the instance is created on activation and freed on deactivation, so a summon lasts exactly as long as the node does.

They are a 2D/3D pair even though a scene carries its own dimension, because the *transform* the graph hands them does not: Position and Rotation are a `Vector3` and a `Quaternion` in 3D, and a `Vector2` and an angle in radians in 2D.

### Settings

| Setting | Values | Meaning |
|---|---|---|
| **Parent** | `CurrentScene`, `Entity`, `Node` | Where the instance is added. `Entity` uses the Parent Entity row's node, `Node` uses the Parent Node row. |
| **Pass ownership** | checkbox, default on | Calls `IInstantiationReceiver.OnInstantiated(owner, source)` on the instance with the ability's owner and source, which is how a spawned `ForgeProjectile3D` learns who fired it. |

### Inputs

| Index | Label | Type | Notes |
|---|---|---|---|
| 0 | Scene | `PackedScene` | Required. Authored with the **Constant** scene resolver, or read from a `Scene` variable. |
| 1 | Position | `Vector3` / `Vector2` | Optional. **Unbound means "where the caster is"** — the Parent Entity's own node — rather than the world origin, which is the sane default for an instance that only cares about rotation. |
| 2 | Rotation | `Quaternion` / `double` | Optional. Unbound, and in 3D a zero quaternion, leave the scene's authored rotation. |
| 3 | Parent Entity | `IForgeEntity` | Optional, defaults to the ability's owner. What the instance is parented to under `Entity` parenting, **and what an unbound Position is read from in every mode**. |
| 4 | Parent Node | `Node` | Optional. Only read under `Node` parenting. |
| 5 | Lifetime | `double` | **State nodes only.** Seconds before the node self-deactivates. Zero or less means "until aborted". |

### Outputs and ports

| Kind | Index | Name | Type | Notes |
|---|---|---|---|---|
| Output | 0 | Instance | `Node` | What was spawned. |
| Output | 1 | Instance Entity | `IForgeEntity` | The entity on the instance, when it has one. |
| Port | 4 | OnLifetimeEnd | Event | **State nodes only.** Emits when the lifetime elapses, as opposed to an abort. |

**Placement happens before parenting.** The transform is applied as a *local* one through the parent's transform, because `AddChild` readies the instance and a scene that measures its own position in `_Ready` — `ForgeProjectile3D` recording where it launched from — has to see the position it was actually spawned at.

## Queue Free

Frees a node from the scene, guarded against a node that has already been freed. Unpaired: a node to free is a node in either dimension.

| Index | Label | Type | Notes |
|---|---|---|---|
| 0 | Node | `Node` | Required. |

## Reparent

Moves a node under a new parent — the stick-to-target, pick-up and drop primitive.

| Setting | Values | Meaning |
|---|---|---|
| **Keep World Transform** | checkbox, default on | Off keeps the node's position *relative to its parent*, which is what attaching to a hand or a socket marker wants. |

| Index | Label | Type | Notes |
|---|---|---|---|
| 0 | Node | `Node` | Required. The node that moves. |
| 1 | New Parent | `Node` | Required. The node it moves under. |

Neither row falls back to the ability's owner, unlike the [interop nodes](interop-nodes.md): "reparent me" and "reparent onto me" are different questions and an empty row reads as neither. The three reparents Godot rejects outright — a node with no parent, a node moved onto itself, and a node moved under its own descendant — are checked here first and reported as authoring warnings instead of engine errors.

## Godot groups

A Godot group is a name attached to scene nodes, and it is the one way to reach a set assembled by hand across a level — every spawn point, every cover marker, a whole patrol. Reading a group is the [`Nodes In Node Group` and `Entities In Node Group` resolvers](../resolvers/scene-graph-resolvers.md#godot-groups); these three nodes write to one.

| Node | Arch | Setting | Input | Behavior |
|---|---|---|---|---|
| `AddToNodeGroupNode` | Action | **Group** (text) | Node (required) | Puts the node in the group permanently. |
| `RemoveFromNodeGroupNode` | Action | **Group** (text) | Node (required) | Takes it out permanently. |
| `NodeGroupNode` | State | **Group** (text) | Node (required) | Holds the membership for as long as the node is active, removing it on deactivation *or abort*. |

All three are dimension-neutral: a group is a name in the scene tree, not a physics concept.

**Only a membership the node added is one it removes.** `NodeGroup` checks whether the node was already in the group when it activated, and leaves it alone if it was — a node the level author put in the group was never this ability's to take away.

The consequence is that **the membership lasts exactly as long as the hold that added it**, and a second hold over the same node and group is a no-op in both directions: it adds nothing on activation and removes nothing on deactivation. So the node leaves the group when the *adding* hold ends, whether that is before or after the other one — it is not a reference count, and it is not "whichever ends last" the way the value overrides in [Interop Nodes](interop-nodes.md) are. A group is a set, so the second hold has nothing to add and nothing to give back; two abilities that need to mark the same node independently want a group each.

## Related Docs

- [Nodes Reference](README.md)
- [Scene Graph and Interop Resolvers](../resolvers/scene-graph-resolvers.md) — `Constant` (scene), `Node Path`, `Node From Entity`, the group readers
- [Interop Nodes](interop-nodes.md)
- [Forge Nodes](../../nodes.md#forgeprojectile2d--forgeprojectile3d) — `ForgeProjectile3D`, the scene these nodes most often spawn
