# Physics Query Nodes

> **Namespace:** `Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Condition` and `.State`
>
> **Add Node group:** Condition → **Physics**, State → **Physics**

These nodes are **Godot-only** — there is no core Forge counterpart, so this page is their reference rather than a Godot-authoring supplement to one.

They ask the physics world a question. Which shape a question takes decides whether it is a node or a [resolver](../resolvers/physics-queries.md):

- **Compound results are nodes.** A cast reports a position, a normal, an entity, a node and a distance; one query writes all five atomically, so downstream reads cannot disagree with each other.
- **Entity-list queries are resolvers**, so they feed `ForEach`, `Where` and `OrderBy` directly.
- **Boolean checks that must work inside a lambda are resolvers.** [`Line Of Sight 3D`](../resolvers/physics-queries.md#narrowing-and-testing) has to be usable inside a `Where`; the node form on this page is for when you want the transitions and the blocker.

Every node here is a 2D/3D pair and takes the two operands the whole family shares. **The one-shot Condition nodes cast the moment a message reaches them**; only the monitored State nodes below poll, and those poll on the fixed step.

- **Mask (`int`, optional).** A collision mask, authored as a number. **Zero means every layer** — a mask of zero can never find anything, so reading it literally would make an unbound row silently disable the query it belongs to.
- **Ignore (`IForgeEntity[]`, required, seeded).** The entities the query keeps off. It is a *list* and not a flag because both ends of a query sit inside a body: a ray from a character's own position starts at its feet, outside its own capsule by a hair, and a line drawn to `%CastPoint` on a target ends inside the target. A fresh row is seeded with the ability's owner — or with the owner and the target for the sight forms — so what the editor shows is what runs. Emptying it is how a query that reports everyone is authored.

> The two mechanisms behind that one row differ by necessity: **casts exclude by RID**, because they must keep off a collider they would otherwise start inside, while **overlaps drop the entities from their results**. Nothing about authoring them differs.

## One-shot casts (Condition)

Both route the message to **True** when they hit and **False** when they do not, which spares every graph an Action-plus-Expression pair just to branch on a hit.

### Raycast

| Setting | Values |
|---|---|
| **Hit Areas** | checkbox, default off |
| **Hit From Inside** | checkbox, default off |

| Index | Label | Type | Notes |
|---|---|---|---|
| 0 | Origin | `Vector3` | Required. |
| 1 | Direction | `Vector3` | Required. |
| 2 | Max Distance | `double` | Required. |
| 3 | Mask | `int` | Optional. |
| 4 | Ignore | `IForgeEntity[]` | Seeded with the owner. |

Hitscan shots, line-of-sight gates, ground snap.

### Shapecast

A raycast with thickness. A fast projectile aimed at a thin target slips past a ray; a dash checked with one walks a character's shoulders into a wall.

| Setting | Values |
|---|---|
| **Hit Areas** | checkbox, default off |

| Index | Label | Type | Notes |
|---|---|---|---|
| 0 | Shape | `Shape3D` | Required, seeded with a [Sphere](../resolvers/shapes.md). |
| 1 | Origin | `Vector3` | Required. |
| 2 | Direction | `Vector3` | Required. |
| 3 | Max Distance | `double` | Required. |
| 4 | Rotation | `Quaternion` | Optional. |
| 5 | Mask | `int` | Optional. |
| 6 | Ignore | `IForgeEntity[]` | Seeded with the owner. |

**A swept cast is two queries.** `CastMotion` reports how far the shape got as a fraction and says nothing about what stopped it; `GetRestInfo` reports what is touching and takes no motion. The node runs the first, places the shape at the fraction it came back with, and runs the second there — with a collision margin, because a sweep stops the shape exactly touching and an exact touch is the one distance floating point cannot be relied on to report as a contact.

### The five outputs

Both casts, and both of their monitored counterparts, write the same five in the same order and with the same names, so a graph swapped from a ray to a sweep keeps every binding it had.

| Index | Name | Type |
|---|---|---|
| 0 | Hit Position | `Vector3` |
| 1 | Hit Normal | `Vector3` |
| 2 | Hit Entity | `IForgeEntity` |
| 3 | Hit Node | `Node` |
| 4 | Distance | `double` |

## Monitored casts (State)

`Ray3DNode` and `Sweep3DNode` are the held forms of Raycast and Shapecast — **the shorter name is the monitored one in both pairs**. They re-resolve every input each step, so bound resolvers track whatever is moving, and they write the same five outputs.

| Setting | Values | Meaning |
|---|---|---|
| **Hit Areas** | checkbox | As the one-shot forms. |
| **Hit From Inside** | checkbox | `Ray` only. |
| **One Shot** | checkbox, default off | Deactivate after the first hit. |

| Port | Index | Kind | Emits |
|---|---|---|---|
| OnHit | 4 | Event | On the transition into hitting something. |
| OnLost | 5 | Event | On the transition back to clear. |
| WhileHit | 6 | Subgraph | Active for as long as it is hitting. |
| WhileClear | 7 | Subgraph | Active for as long as it is not. |

Beams, aim-lock, tether break; a dash that cancels the moment its corridor closes.

## Overlap

Watches a volume and reports the entities that enter and leave it. The port shape mirrors core's `ConditionMonitorNode`: two edges for the transitions, two subgraphs for the states between them — **the edges are per entity**, the subgraphs follow occupancy.

| Setting | Values | Meaning |
|---|---|---|
| **Source** | `ExistingArea`, `TransientShape` | Whether the volume is an `Area3D` in the scene or a shape the query builds. |
| **Area** | text, placeholder `%WeaponHitbox` | `ExistingArea` only. Empty means the entity's own node. |
| **Include Areas** | checkbox, default off | Whether overlapping areas count as well as bodies. |

**The Source setting decides which rows exist.** The editor shows only the ones its mode reads, so a row you cannot see is one that mode would have ignored.

| Index | Label | Type | Mode | Notes |
|---|---|---|---|---|
| 0 | Entity | `IForgeEntity` | ExistingArea | Optional. Says *whose* area to read. |
| 1 | Shape | `Shape3D` | TransientShape | Seeded with a Sphere. |
| 2 | Position | `Vector3` | TransientShape | **Required**, seeded with `Entity Position 3D` so a fresh node means "around me". |
| 3 | Rotation | `Quaternion` | TransientShape | Optional. |
| 4 | Mask | `int` | TransientShape | Optional. An existing area is filtered by **its own** collision mask in the scene, so the node has no mask to offer there. |
| 5 | Poll Interval | `double` | both | Optional. Seconds between polls; unbound polls every fixed step. |
| 6 | Ignore | `IForgeEntity[]` | both | Seeded with the owner. |

| Kind | Index | Name | Notes |
|---|---|---|---|
| Output | 0 | Event Entity | Which entity an entered or exited event is about. |
| Port | 4 | OnEntered | Once per entity that enters. |
| Port | 5 | OnExited | Once per entity that leaves. |
| Port | 6 | WhileOverlapping | Subgraph, active while anything at all is inside. |
| Port | 7 | WhileEmpty | Subgraph, active while nothing is. |

**Both modes poll and diff; neither subscribes to the area's signals.** One diff routine means the two modes report entry and exit identically, and it is what makes an entity with several colliders count once — a hurtbox leaving while a hitbox stays is not an exit. It also removes a connect/disconnect lifetime that would have to survive aborts and assembly reloads.

**The first poll runs during activation**, so a trap triggers on the frame it arms, one entity per event. Because the node polls on the physics step and Godot's overlap list is per physics step, nothing is missed at any frame rate — at the default Poll Interval of zero the two rates are the same one. An authored interval is what skips overlaps that start and end between polls.

Traps, melee hit windows, proximity triggers, auras.

## Line Of Sight

The node form of the [sight resolver](../resolvers/physics-queries.md#narrowing-and-testing): the same test, with the transitions named and the blocker reported. A `ConditionMonitorNode` over the resolver gets the edges but not the blocker, and names its ports for a question nobody phrases as true and false.

| Setting | Values |
|---|---|
| **Deactivate When Blocked** | checkbox, default off |

| Index | Label | Type | Notes |
|---|---|---|---|
| 0 | From | `Vector3` | Required. Bind it to [`Entity Position 3D`](../resolvers/spatial-getters.md), which is what the resolver form seeds. |
| 1 | To | `Vector3` | Required. |
| 2 | Ignore | `IForgeEntity[]` | Required, seeded with an array of the owner **and** the target. |
| 3 | Mask | `int` | Optional. |

The Ignore row is required rather than optional here on purpose: leaving it unbound would have meant "ignore the owner and the target", which an array of exactly those two already spells. `IsOptional` exists only for inputs whose absence no resolver can reproduce.

| Kind | Index | Name |
|---|---|---|
| Output | 0 | Blocker Entity |
| Output | 1 | Blocker Node |
| Output | 2 | Block Position |
| Port | 4 | OnClear |
| Port | 5 | OnBlocked |
| Port | 6 | WhileClear (subgraph) |
| Port | 7 | WhileBlocked (subgraph) |

Channels that break on cover, tethers, aggro drop.

## Masks are operands, not spinboxes

Node settings render as enums, checkboxes and text — there is no numeric setting control — so a mask, a poll interval and a max distance are all **inputs**. That is the better answer anyway: a number that cannot come from a variable or scale with an ability level is the weaker half of the pair.

## Filtering is composition

No query node takes a filter or a predicate. Core's element-lambda resolvers already do this three ways: gate an event edge with `ExpressionNode` plus an `AttributeResolver`, filter an array with `ObjectWhereResolver` and `ElementEntityResolver`, or track a filtered population with `ConditionMonitorNode` over `Count(Where(Overlap3D(...))) > 0`.

## What differs in 2D

The whole family mirrors mechanically, with one difference: **`Overlap2D`'s and `Sweep2D`'s Rotation operands are angles in radians** rather than quaternions. The rotation guard disappears with them — every 3D query has to reject the zero quaternion an unfilled operand resolves to, while an unfilled angle is zero, which means "unturned".

## Related Docs

- [Nodes Reference](README.md)
- [Physics Query Resolvers](../resolvers/physics-queries.md) — the entity-list and boolean forms
- [Shape Resolvers](../resolvers/shapes.md) — what feeds a Shape row
- [Physics Nodes](physics-nodes.md) — the writing half
- [Physics Debug Drawing](../physics-debug-drawing.md)
