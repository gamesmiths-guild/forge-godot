# Scene Graph and Interop Resolvers

> **Namespace:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers`
>
> **Runtime:** `Gamesmiths.Forge.Godot.Core.Statescript.Resolvers`

These resolvers are **Godot-only** — there is no core Forge counterpart, so this page is their reference rather than a Godot-authoring supplement to one.

They move between the three things a Godot graph deals in — an entity, a scene node, and a `PackedScene` — and read a node's own state. All of them are dimension-neutral.

## Scenes and nodes as values

| Resolver | Output | Rows |
|---|---|---|
| **Constant** (scene) | `PackedScene` (object type `Scene`) | An exported `PackedScene`, scalar or array |
| **Constant** (node path) | `Node` (object type `GodotNode`) | A **Node Path** string |

The scene picker is the only way a `.tscn` reaches a graph, since node settings carry primitives only. It feeds [`Instantiate Scene`](../nodes/scene-nodes.md#the-instantiating-pair) and `Scene`.

The node path resolver is the node lane's constant, so a hand-placed container or marker is reachable without a variable something else had to write. Absolute paths and `%` unique names are supported, and a `%name` is resolved **outward from the graph's own entity** rather than down from the current scene — see [node paths](../nodes/spatial-nodes.md#node-paths).

## Crossing between lanes

| Resolver | Output | Rows | Notes |
|---|---|---|---|
| **Node From Entity** | `Node` | Of (entity); **Node** (path) | The entity's nearest spatial ancestor of *either* kind. |
| **Entity From Node** | `Entity` | **Node** (nested) | What a spawn wrote to a variable, or a ray reported as its collider, becomes something effects can be applied to. |
| **Entity At Path** | `Entity` | **Node Path** | `Entity From Node` over a node path in one row. |

`Node From Entity` resolves through the bridge's `TryGetOwningNode`, the same lookup the [interop nodes](../nodes/interop-nodes.md#the-node-row) use for their unbound Node row, so the two cannot drift. **A path that misses resolves to null rather than falling back**, because the path names *which* node is wanted — unlike the [spatial getters](spatial-getters.md#shared-rows), where the path is an offset from a subject that is already known.

**`Entity At Path` is a deliberate exception to the "cut as already composable" rule.** It is exactly `Entity From Node` over a node path and should by that rule have been cut. It ships anyway because naming a character already in the level is common enough that two nested pickers every time is friction rather than composition. The test is not "can this be composed" but "is it composed often enough that the composition becomes the thing" — and when the answer is yes, the shortcut ships **beside** the parts, never instead of them.

## Reading a node's own state

**Node Property** is the read escape hatch, and the counterpart to [`Set Node Property`](../nodes/interop-nodes.md#properties).

| Row | Meaning |
|---|---|
| **Node** | Nested, seeded with `Node From Entity`, so a fresh one reads "a property on me". |
| **Property** | The property path. |
| **Type** | One of the [interop conversion set](../nodes/interop-nodes.md#the-conversion-set), seeded from the slot's own expected type. |

**There is no array checkbox.** A resolver is handed the shape of the slot it sits in, so the same Node Property reads an array in an array input and a value in a scalar one, and the resource records what it was told rather than what anyone authored. The type row is seeded the same way, which leaves a choice to make only in the wildcard operands — the sides of a comparison, a math operand — where the slot genuinely does not say.

**A bad property path is reported, not guessed at.** `GetIndexed` answers an unknown path with nothing at all, so the read checks that the object declares the path's first segment — but only when the value comes back nil, since a resolver runs every tick. For a value-lane read nil is already impossible from a declared property, and for the object lane an unset reference is a legitimate answer that costs one scan to confirm.

## Entity hierarchy

| Resolver | Output | Rows |
|---|---|---|
| **Parent Entity** | `Entity` | Of (entity) |
| **Child Entities** | `Entity[]` | Of (entity) |

Projectile-sub-entity to caster patterns, and the summoner acting on everything it summoned.

**Both step over the levels that resolve back to the entity being read.** Under the composition pattern an entity's node is a *child* of its body, so the body is a level whose children include the entity itself — `Parent Entity` climbing one level and asking "is there an entity here" would get itself back and report everything as its own parent.

`Child Entities` also **stops descending at each entity it finds**, because what is inside a turret belongs to the turret: a builder asking for its children should not be handed its turrets' passengers, and one more `Child Entities` on the turret is how those are reached.

## Godot groups

A Godot group is a name attached to scene nodes, and it is the one way to reach a set assembled by hand across a level — every spawn point, every cover marker, a whole patrol, none of which a path or a hierarchy walk can name.

| Resolver | Output | Row |
|---|---|---|
| **Nodes In Node Group** | `Node[]` | **Group** |
| **Entities In Node Group** | `Entity[]` | **Group** |

**`Entities In Node Group` returns a set, not a list.** A project that puts both a body and its hurtbox in one group has named two nodes and one entity, and a `ForEach` over the result would apply everything to it twice. `Nodes In Node Group` has no such problem and returns them as they come.

Writing to a group is the [group nodes](../nodes/scene-nodes.md#godot-groups): `Add To Node Group`, `Remove From Node Group`, and `Node Group` for a membership that lasts exactly as long as an ability.

The family is named for what a group *is* rather than for what a node does to one. `Group Override` would have described nothing, and plain "Group" invites a reader arriving from the tags or effects side to assume a Forge concept. `NodeGroup` carries the Godot meaning in the name, at the cost of a class called `NodeGroupNode` and a resolver called `Nodes In Node Group` — the price of a family whose names all say the same word.

## Is Valid, extended

The Godot layer subclasses core's `IsValidResolver` so one validity question in the editor is not two that each answer half of it. See [IsValidResolver](is-valid-resolver.md).

## Related Docs

- [Resolvers Reference](README.md)
- [Interop Nodes](../nodes/interop-nodes.md) — the writing half
- [Scene Nodes](../nodes/scene-nodes.md) — spawning, freeing, reparenting, groups
- [Spatial Getters](spatial-getters.md)
- [Variables and Data](../variables.md) — object variable types
