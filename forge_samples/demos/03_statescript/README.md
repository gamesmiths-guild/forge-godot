# Demo 03 — Statescript

The Real-Time 3D demo's four player abilities, its enemy and its HUD, rebuilt so that every ability behavior is a **Statescript graph** instead of a C# `IAbilityBehavior`. Open the two demos side by side: where `01_realtime_3d` has a class, this one has a graph resource under `graphs/`.

Movement is WASD or the arrow keys. Aim is the mouse.

## Before running it

`MovementAttributes` is an **attribute set definition** (`attributes/movement_attributes.tres`) rather than a hand-written C# class. The generated class ships in `res://forge_generated/attribute_sets/`; if it is ever missing, open the definition, press **Regenerate Attribute Set Code** (or just save it), and build.

`Speed` is stored with two decimal places, so `500` is 5.00 units per second — the scaled-integer convention Forge uses for fractional stats. The player has 500, the enemy 200, the same speeds the Real-Time 3D demo hardcodes; the controller and the `enemy_brain` graph both divide by 100 before moving anything.

## The graphs

| Graph | Who runs it | What it does |
| --- | --- | --- |
| `projectile` | Player, Mouse 1 | Commits cost and cooldown and instantiates `scenes/projectile.tscn` — a `ForgeProjectile3D` — at the cast point, facing the way the player faces. The player already faces the cursor, so nothing in the graph aims. |
| `dash` | Player, Space | Commits, then plays the `roll` clip through a `Play Animation` whose subgraph holds a `Collision Override 3D` that clears the `enemies` mask bit and, under it, a `Move Body 3D` that sweeps the body along the aim at 20 units per second, sliding along whatever it hits — nothing writes a velocity. Both are torn down with the clip, and the move's destination sits 10 units out, further than the clip's 0.27 s can carry it, so the roll's length is the dash's length. `movement.block` and `immunity.damage` come from the ability's activation-owned tags, not the graph. |
| `shield` | Player, 3 | A toggle built from holders: an `Effect` (80% less incoming damage), a `Cue` (the shield VFX), an `Event Listener` on `event.damage.taken` that commits the cost again per hit and exits when it cannot, an `Attribute Listener` that exits under 10 mana, and an `Input Action` on `skill_3` that exits on the next press. |
| `thorns` | Player, passive | Triggered by `event.hit.melee`. Doubles the event magnitude into a set-by-caller damage effect and applies it to every entity in `%ReflectArea` with a distance falloff, then executes the reflect VFX cue and `cue.screen.shake`, which the level's `CameraShakeCueHandler` answers. |
| `respawn` | Player, passive | Remembers where the player stood when the level readied; a `Tag Listener` on `state.fallen` puts the player back there and zeroes its velocity. |
| `enemy_attack` | Enemy | Commits the 1.5 s cooldown and applies `enemy_damage` to the ability target. The cooldown effect also grants `movement.block`, so the chase stays paused for 1.5 s after each swing even if the player steps out of range. |
| `enemy_brain` | Enemy, passive | An `Overlap 3D` on `%AttackRange` is the whole state machine: while empty, a `Condition Monitor` (no `movement.block`) owns a `Nav Move To 3D` chase toward `%Player`; while overlapping, a `Loop Timer` keeps trying the attack. An `Attribute Listener` that sees Health reach zero and a `Tag Listener` on `state.fallen` both end in `Queue Free`. |
| `brazier` | Brazier, passive | A `lit` bool. A `Condition Monitor` on it holds, while true, `Node Property Override`s that show the flame and light the lamp and a `Loop Timer` that spawns `scenes/enemy.tscn` every 5 s at a random `spawn_points` marker while fewer than six enemies are alive. An `Overlap 3D` on `%Trigger` owns an `Input Action` on `interact` whose press flips the bool. |

Two things the graphs rely on that are worth knowing:

- **Passive abilities wake through a tag.** `respawn`, `enemy_brain` and `brazier` have trigger source `TagPresent` on `state.awake`. Each entity's `grant_abilities.tres` grants its abilities and then, as its second component, adds `state.awake` — the grant subscribes the trigger first, the tag fires it. That is the whole "runs from the moment the entity is ready" recipe, and it works the same for a hand-placed entity and a spawned one.
- **Thorns triggers on `event.hit.melee`, not on `event.damage.taken`.** The Real-Time 3D demo's C# reflects only `DamageType.Physical`; a graph cannot read an enum payload, so `abilities/enemy/enemy_damage.tres` raises `event.hit.melee` through a `RaiseEvent` component instead, and fire ticks never reflect. The event carries the damage actually dealt, not the 100 the effect asks for: an `Attribute Accumulator` component ahead of it in the list tallies the health the hit removed — after the shield's mitigation and the health floor — and publishes it as the set-by-caller magnitude the event reads, which is the `finalDamage` the Real-Time 3D demo reflects.

## The stations

| Station | Where | What it shows |
| --- | --- | --- |
| Bonfire | centre | Walk in and catch fire (`fire.tres`). Shoot a projectile through it: the projectile is an entity with `trait.flammable`, so it catches fire and spreads it to whatever it hits. |
| Lake | north-west | A basin cut into the plateau; being in the water douses fire (`water.tres`). **No graph at all** — an `EffectArea3D` and a resource. |
| Regen area | north-east | Instant heal plus stacking regeneration. No graph either. |
| Brazier | east | Press **E** inside the ring to start enemy waves, and again to stop them. |
| Dummies | by the bonfire | Two static targets with fast regeneration (`effects/dummy_regen.tres`, 100 health a tick), so they can be shot at indefinitely. |
| Spawn markers | beyond the chasm | Six `Marker3D`s in the Godot group `spawn_points`. Enemies spawn there and path over the bridge. |

Falling off the bridge or the rim lands in `%FallZone`, an `EffectArea3D` that applies `effects/fallen.tres` (`state.fallen`) while something is inside. The player's `respawn` graph answers the tag by teleporting home; an enemy's `enemy_brain` answers it with `Queue Free`.

## Layout

| Path | Contents |
| --- | --- |
| `abilities/` | `ForgeAbilityData` per ability, each pointing at a graph through `StatescriptAbilityBehavior`, plus the cost, cooldown and damage effects and one `grant_abilities.tres` per entity type. |
| `attributes/` | The `MovementAttributes` definition. |
| `effects/` | `fallen.tres` and `dummy_regen.tres`. |
| `graphs/` | The eight `StatescriptGraph` resources. |
| `materials/` | The palette and the `WorldEnvironment`. No textures. |
| `scenes/` | `sample_level.tscn` (the level, shared with future demos), `player.tscn`, `enemy.tscn`, `projectile.tscn`, one scene per station plus the target dummy and the rim stone under `stations/`, trees under `props/`, and the baked `sample_level_navmesh.tres`. |
| `scripts/` | The only C# in the demo: movement, a puppet body, and a camera rig. |

The level is a composition root and nothing else: a `NavigationRegion3D` baked from the static colliders in the `navigation_mesh_source_group` group, a `%Spawned` container that every runtime-created node is parented to (so it is freed with the level and can see the level's unique names), the effect areas, the HUD and the cue handlers. Collision layers are `world`, `player`, `enemies` and `projectiles`; the dummies sit on `enemies`.

## The only C# here

| Script | Job |
| --- | --- |
| `PlayerController3D` | Movement with gravity, facing the cursor every physics tick (the same `AimActivationData.FromMouseGround` sample the abilities are activated with, so a graph reading `Entity Rotation 3D` is already aimed), and the same HUD mirror `Character3D` keeps in the Real-Time 3D demo. It never decides what an ability does. While `movement.block` is on it neither steers nor turns and drops its own horizontal velocity, so the roll's `Move Body 3D` is the only thing moving the body and it stays pointed where it was aimed. |
| `PuppetBody3D` | Every non-player body. Gravity and `MoveAndSlide()`; `Nav Move To 3D` writes the velocity. |
| `FollowCamera3D` | A damped follow rig with a fixed top-down view. It moves the rig, never the camera's own offset — that belongs to `CameraShakeCueHandler`. |

The view scripts (`ActionBarView`, `HealthBar3D`, the floating text and particle cue handlers) and the VFX scenes are the Real-Time 3D demo's, referenced as they are. `SpreadFireAbilityBehavior`, which `fire.tres` grants to anything burning, is also still that demo's C#.

## Still to come

Animation clips, audio and the rest of the juice, once the two demos are at parity.
