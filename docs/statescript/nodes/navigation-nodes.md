# Navigation Nodes

> **Runtime Types:** `Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State.NavMoveTo3DNode` / `NavMoveTo2DNode`
>
> **Add Node group:** State → **Navigation**

These nodes are **Godot-only** — there is no core Forge counterpart, so this page is their reference rather than a Godot-authoring supplement to one.

`Nav Move To` steers an entity to a destination along a navigation path. It writes the body's velocity from the agent each fixed step and lets the game move it — the layering that keeps an ability out of the character controller's way.

## Settings

| Setting | Kind | Meaning |
|---|---|---|
| **Agent** | text, placeholder `NavigationAgent3D` | Path to an existing agent child. Empty takes the entity's own, matching the [presentation nodes](presentation-nodes.md#the-player-path). |
| **Use Safe Velocity** | checkbox, default off | Steer with the agent's avoidance result instead of the raw path direction. |

**The agent is authored and never created.** Avoidance radius and navigation layers are scene data belonging to the character, not to one ability. Turning on **Use Safe Velocity** for an agent with avoidance disabled warns and falls back to following the path directly, rather than steering with a velocity the agent never computes.

## Inputs

| Index | Label | Type | Notes |
|---|---|---|---|
| 0 | Entity | `IForgeEntity` | Optional. Unbound means the ability's owner. |
| 1 | Target | `Vector3` / `Vector2` | Required. **Re-read every step**, so binding it to a spatial getter makes this a chase. |
| 2 | Speed | `double` | Required — an unbound row resolves to zero, which cannot drive a path. A negative speed is read as zero. |

## Ports

| Index | Name | Emits |
|---|---|---|
| 4 | OnReached | The agent arrived. |
| 5 | OnFailed | The destination is unreachable, or there is no agent to steer with. |

**Deactivating zeroes the body's velocity**, which covers arrival, failure and abort with one rule rather than three.

## An unsynced map reports everything as unreachable

The delicate part is not the avoidance callback. `NavigationServer.MapGetPath` hands back an empty path from a navigation map that has not synced yet, and an empty path makes `IsTargetReachable` report *every* destination as unreachable — so a walk ordered on the frame its level loaded would fail instantly, for no reason a player could see.

The node stamps the physics frame it activated on and does not judge reachability until that number has moved.

## Asking navigation questions

The node walks; it does not answer. Branching on reachability, measuring how far a walk would be, or clamping a ground-targeted point onto walkable floor are the [navigation resolvers](../resolvers/navigation-resolvers.md).

## Related Docs

- [Nodes Reference](README.md)
- [Navigation Resolvers](../resolvers/navigation-resolvers.md) — `Nav Reachable`, `Nav Path Length`, `Nav Closest Point`
- [Spatial Nodes](spatial-nodes.md) — `Move To` and `Move Body`, the non-pathfinding moves
