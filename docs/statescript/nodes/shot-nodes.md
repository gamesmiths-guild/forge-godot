# Shot Nodes

> **Namespace:** `Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action`
>
> **Add Node group:** Action → **Shots**
>
> **Network kind:** Simulation

These nodes are **Godot-only**. They fire [shots](../../shots.md) from the ability's owner: a hitscan, a lightweight projectile or a blast. Each takes its shot as a resource, picked with a **Constant** row, and applies the resource's effects itself, to each entity the shot hits, with the share of strength the shot had left as float context data.

They're simulation: the server and the client that predicts the owner both run them. On the server, a client's shot meets the other entities where that client was shown them, and what it hits shows its cues on the shooter's screen at once. Pellets spread from the owner's seeded random, so both machines spread them alike.

Every node here is a 2D/3D pair: the 2D form takes `Vector2` where the 3D one takes `Vector3`.

## Hitscan

Fires a [`ForgeHitscanData`](../../resources.md#forgehitscandata): its pellets hit what is on their lines at once, going through what its penetration lets them, and its effects go to each entity a pellet hits. With `Hits Projectiles` on, a pellet also hits lightweight projectiles that can be shot, where the shooter saw them, and ends them as struck.

| Index | Label | Type | Notes |
|---|---|---|---|
| 0 | Hitscan | `ForgeHitscanData` | Required. |
| 1 | Origin | `Vector3` | Required. Where the shot starts. |
| 2 | Aim | `Vector3` | Required. Where it aims. |

| Output | Type | Notes |
|---|---|---|
| End | `Vector3` | Where the first pellet stopped, or the end of its range: where a tracer ends. |
| Normal | `Vector3` | The normal of the surface the first pellet stopped on, or zero. |
| Entity | `IForgeEntity` | The first entity a pellet hit. |
| Hitbox | `Node` | The first [hitbox](../../shots.md#hitboxes) a pellet hit. |
| Hits | `int` | How many hits on entities the pellets made. |

## Launch Projectile

Launches a [`ForgeProjectileData`](../../resources.md#forgeprojectiledata) in the owner's current tick. The owner's ticks fly it, so it leaves the shooter's hand at once on the shooter's screen, and other clients are sent the launch.

| Index | Label | Type | Notes |
|---|---|---|---|
| 0 | Projectile | `ForgeProjectileData` | Required. |
| 1 | Origin | `Vector3` | Required. |
| 2 | Direction | `Vector3` | Required. |
| 3 | Speed | `double` | Optional. Unbound uses the projectile's `Speed`. |

## Splash

Blasts a [`ForgeSplashData`](../../resources.md#forgesplashdata) from a point: each entity in reach gets its effects and push, scaled by how close it is.

| Index | Label | Type | Notes |
|---|---|---|---|
| 0 | Splash | `ForgeSplashData` | Required. |
| 1 | Center | `Vector3` | Required. |

| Output | Type | Notes |
|---|---|---|
| Hits | `int` | How many entities it reached. |
