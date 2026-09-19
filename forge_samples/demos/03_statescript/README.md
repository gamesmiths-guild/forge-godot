# Demo 03 — Statescript

The Real-Time 3D demo's four player abilities, its enemy and its HUD, rebuilt so that every ability behavior is a **Statescript graph** instead of a C# `IAbilityBehavior`. Open the two demos side by side: where `01_realtime_3d` has a class, this one has a graph resource under `graphs/`.

Movement is WASD or the arrow keys. Aim is the mouse.

## Before running it

`MovementAttributes` is an **attribute set definition** (`attributes/movement_attributes.tres`) rather than a hand-written C# class. The generated class ships in `res://forge_generated/attribute_sets/`; if it is ever missing, open the definition, press **Regenerate Attribute Set Code** (or just save it), and build.

`Speed` is stored with two decimal places, so `500` is 5.00 units per second — the scaled-integer convention Forge uses for fractional stats. The player has 500, the enemy 200, the same speeds the Real-Time 3D demo hardcodes; the controller and the `enemy_brain` graph both divide by 100 before moving anything.

## The graphs

| Graph | Who runs it | What it does |
| --- | --- | --- |
| `projectile` | Player, Mouse 1 | Commits cost and cooldown and instantiates `scenes/projectile.tscn` — a `ForgeProjectile3D` — at the cast point, facing the way the player faces. The player already faces the cursor, so nothing in the graph aims. A `Play Audio One Shot` then plays the player's `ShotSfx`, and a `Play Animation One Shot` the `shoot` recoil on its `FxPlayer`. |
| `dash` | Player, Space | Commits, plays `DashSfx` through a `Play Audio One Shot`, then plays the `roll` clip through a `Play Animation` whose subgraph holds a `Collision Override 3D` that clears the `enemies` mask bit and, under it, a `Move Body 3D` that sweeps the body along the aim at 20 units per second, sliding along whatever it hits — nothing writes a velocity. Both are torn down with the clip, and the move's destination sits 10 units out, further than the clip's 0.27 s can carry it, so the roll's length is the dash's length. `movement.block` and `immunity.damage` come from the ability's activation-owned tags, not the graph. |
| `shield` | Player, 3 | A toggle built from holders: an `Effect` (80% less incoming damage), a `Cue` (the shield VFX and its loop), an `Event Listener` on `event.damage.taken` that plays `ShieldHitSfx` and commits the cost again per hit, exiting when it cannot, an `Attribute Listener` that exits under 10 mana, and an `Input Action` on `skill_3` that exits on the next press. |
| `thorns` | Player, passive | Triggered by `event.hit.melee`. Doubles the event magnitude into a set-by-caller damage effect and applies it to every entity in `%ReflectArea` with a distance falloff, then executes the reflect VFX and sound cues and `cue.screen.shake`, which the level's `CameraShakeCueHandler` answers. |
| `respawn` | Player, passive | Remembers where the player stood when the level readied; a `Tag Listener` on `state.fallen` plays `FallSfx` and puts the player back there with its velocity zeroed. |
| `enemy_attack` | Enemy | Commits the 1.5 s cooldown, turns to face the target with a `Set Rotation Toward 3D` (the only time the enemy faces anything — the capsule has no front otherwise), lunges through a `Play Animation One Shot` of the `attack` clip, plays `SwingSfx` and applies `enemy_damage` to the ability target. The cooldown effect also grants `movement.block`, so the chase stays paused for 1.5 s after each swing even if the player steps out of range. |
| `enemy_brain` | Enemy, passive | An `Overlap 3D` on `%AttackRange` is the whole state machine: while empty, a `Condition Monitor` (no `movement.block`) owns a `Nav Move To 3D` chase toward `%Player` and a `Loop Timer` that restarts it every half second — a walk ends when it arrives or cannot path, as it does while the player is falling below the navmesh, and a chase must not stay ended; while overlapping, a `Loop Timer` keeps trying the attack. An `Attribute Listener` that sees Health reach zero instantiates `scenes/vfx/death_burst.tscn` where the enemy stands and then `Queue Free`s it, and a `Tag Listener` on `state.fallen` goes through the same burst, so an enemy that falls dies the same way. |
| `brazier` | Brazier, passive | A `lit` bool. A `Condition Monitor` on it holds, while true, `Node Property Override`s that show the flame and light the lamp and a `Loop Timer` that, every 5 s while fewer than six enemies are alive, remembers a random `spawn_points` marker in `spawn_position` and sends an ember there: a `Scene 3D` holds `scenes/vfx/spawn_ember.tscn` at `%Flame` for 1.2 s while a `Move To 3D` in its subgraph lobs it to `spawn_position` on a 4-unit arc, and its `OnLifetimeEnd` instantiates `scenes/vfx/spawn_burst.tscn` and then `scenes/enemy.tscn` at the same point. The ember is a bare `ForgeEntity` so a graph can move it. An `Overlap 3D` on `%Trigger` owns an `Input Action` on `interact` whose press flips the bool and plays `ToggleSfx`; switching the brazier off mid-flight frees the ember with the `Scene 3D`, and nothing spawns. |

Two things the graphs rely on that are worth knowing:

- **Passive abilities wake through a tag.** `respawn`, `enemy_brain` and `brazier` have trigger source `TagPresent` on `state.awake`. Each entity's `grant_abilities.tres` grants its abilities and then, as its second component, adds `state.awake` — the grant subscribes the trigger first, the tag fires it. That is the whole "runs from the moment the entity is ready" recipe, and it works the same for a hand-placed entity and a spawned one.
- **Thorns triggers on `event.hit.melee`, not on `event.damage.taken`.** The Real-Time 3D demo's C# reflects only `DamageType.Physical`; a graph cannot read an enum payload, so `abilities/enemy/enemy_damage.tres` raises `event.hit.melee` through a `RaiseEvent` component instead, and fire ticks never reflect. The event carries the damage actually dealt, not the 100 the effect asks for: an `Attribute Accumulator` component ahead of it in the list tallies the health the hit removed — after the shield's mitigation and the health floor — and publishes it as the set-by-caller magnitude the event reads, which is the `finalDamage` the Real-Time 3D demo reflects.

## The stations

| Station | Where | What it shows |
| --- | --- | --- |
| Bonfire | centre | Crackles on its own (an `AudioStreamPlayer3D` on autoplay). Walk in and catch fire (`fire.tres`). Shoot a projectile through it: the projectile is an entity with `trait.flammable`, so it catches fire and spreads it to whatever it hits. |
| Lake | north-west | A basin cut into the plateau; being in the water douses fire (`water.tres`). **No graph at all** — an `EffectArea3D` and two resources: `effects/quench.tres` sits ahead of `water.tres` in the area, an instant effect that requires `effect.fire` and does nothing but execute `cue.vfx.quench` and `cue.sfx.quench`, so the vapor and the hiss only happen to something that was actually burning. |
| Regen area | north-east | Instant heal plus stacking regeneration — each entry adds a stack, up to three, and the swirl thickens with them. No graph either. |
| Brazier | east | Press **E** inside the ring to start enemy waves, and again to stop them. |
| Dummies | by the bonfire | Two static targets with fast regeneration (`effects/dummy_regen.tres`, 100 health a tick), so they can be shot at indefinitely. |
| Spawn markers | beyond the chasm | Six `Marker3D`s in the Godot group `spawn_points`. Enemies spawn there and path over the bridge. |

Falling off the bridge or the rim lands in `%FallZone`, an `EffectArea3D` that applies `effects/fallen.tres` (`state.fallen`) while something is inside. The player's `respawn` graph answers the tag by teleporting home; an enemy's `enemy_brain` answers it with the death burst and `Queue Free`.

## Layout

| Path | Contents |
| --- | --- |
| `abilities/` | `ForgeAbilityData` per ability, each pointing at a graph through `StatescriptAbilityBehavior`, plus the cost, cooldown and damage effects and one `grant_abilities.tres` per entity type. |
| `attributes/` | The `MovementAttributes` definition. |
| `effects/` | `fallen.tres`, `dummy_regen.tres` and `quench.tres`. |
| `graphs/` | The eight `StatescriptGraph` resources. |
| `materials/` | The palette, the `WorldEnvironment` and the two flash materials. No textures. |
| `scenes/` | `sample_level.tscn`, `player.tscn`, `enemy.tscn`, `projectile.tscn`, one scene per station plus the target dummy and the rim stone under `stations/`, trees under `props/`, the demo's own particle scenes under `vfx/`, and the baked `sample_level_navmesh.tres`. |
| `scripts/` | The only C# in the demo: movement, a puppet body, a camera rig and the hit flash. |

The level is a composition root and nothing else: a `NavigationRegion3D` baked from the static colliders in the `navigation_mesh_source_group` group, a `%Spawned` container that every runtime-created node is parented to (so it is freed with the level and can see the level's unique names), the effect areas, the HUD and the cue handlers. Collision layers are `world`, `player`, `enemies` and `projectiles`; the dummies sit on `enemies`.

## The only C# here

| Script | Job |
| --- | --- |
| `PlayerController3D` | Movement with gravity, facing the cursor every physics tick (the same `AimActivationData.FromMouseGround` sample the abilities are activated with, so a graph reading `Entity Rotation 3D` is already aimed), and the same HUD mirror `Character3D` keeps in the Real-Time 3D demo. It never decides what an ability does. While `movement.block` is on it neither steers nor turns and drops its own horizontal velocity, so the roll's `Move Body 3D` is the only thing moving the body and it stays pointed where it was aimed. A press refused for cooldown or mana plays `DryFireSfx`; other refusals stay quiet, because pressing the shield's key while it is up is a refusal too, and that press is the toggle off. |
| `PuppetBody3D` | Every non-player body. Gravity and `MoveAndSlide()`; `Nav Move To 3D` writes the velocity. |
| `FollowCamera3D` | A damped follow rig with a fixed top-down view. It moves the rig, never the camera's own offset — that belongs to `CameraShakeCueHandler`. |
| `HitFlashCueHandler3D` | A cue handler that flashes every mesh under the target with one material for a moment, by writing and then restoring their material overrides. The level has two: white for `cue.vfx.hit`, a shorter orange one for `cue.vfx.burn`. |

The view scripts (`ActionBarView`, `HealthBar3D`, the floating text and particle cue handlers) and the status VFX scenes are the Real-Time 3D demo's, referenced as they are. `SpreadFireAbilityBehavior`, which `fire.tres` grants to anything burning, is also still that demo's C#.

## Juice

Everything that is feedback rather than rules hangs off cues, so the effects and graphs decide *when* and the level decides *how*. The cue tags are on the effects (`hit`, `burn`, `fire`, `wet`, `heal`, `regen` share the cue entries the floating text and the status VFX already had, so they fire on the same terms) and the handlers are nodes in `sample_level.tscn`:

| Cue | Fires when | Handler |
| --- | --- | --- |
| `cue.vfx.hit`, `cue.sfx.hit` | Any damage effect executes (`damage.tres`, `enemy_damage.tres`, `reflect_damage.tres`) | `HitFlashCueHandler3D` with `materials/hit_flash.tres`, 150 ms; `AnimationCueHandler` playing `hit` on the target's `FxPlayer`; `AudioCueHandler` with `hit.ogg` |
| `cue.vfx.burn`, `cue.sfx.burn` | Each fire tick that took health | `HitFlashCueHandler3D` with `materials/burn_flash.tres`, 120 ms; `AudioCueHandler` with `hit.ogg` through a flat `MagnitudeCurve` at 0.35, which is how one clip serves as the quieter tick |
| `cue.sfx.fire` | Fire is applied, until it is removed | `AudioCueHandler` with `fire-loop.ogg`, `PlayOnExecute` off — a periodic effect executes its cues every tick, and without that the loop would restart on top of itself each second |
| `cue.vfx.quench`, `cue.sfx.quench` | `quench.tres` executes in the lake | `ParticlesCueHandler3D` with `vfx/vapor_effect.tscn`; `AudioCueHandler` with `quench.ogg` |
| `cue.sfx.wet` | Wet is applied, until it is removed | `AudioCueHandler` with `water-dripping-loop.ogg` |
| `cue.sfx.heal` | `heal.tres` executes and actually healed, and every regeneration tick that did | `AudioCueHandler` with `heal.ogg`, `PlayOnApply` and `StopOnRemove` off — the tick shares the floating text's cue entry, so a regeneration with nothing left to heal is as silent as it is textless; applying it says nothing until it has done something, and the last tick lands on the frame the effect expires, so its ding is left to finish |
| `cue.sfx.regen` | Regeneration is applied, until it is removed | `AudioCueHandler` with `healing-loop.ogg`, `PlayOnExecute` off |
| `cue.vfx.regen` | Regeneration is applied, and updated per stack | `ParticlesCueHandler3D` with `vfx/regen_effect.tscn`; the stack count drives the emitter's amount ratio and, through the handler's `IntensitySpeed` range, its pace: one stack is a third of the swirl at its authored speed, three the whole of it at two and a half times |
| `cue.sfx.shield` | The `shield` graph's `Cue` holder, for as long as the shield is up | `AudioCueHandler` with `shield-loop.ogg` |
| `cue.sfx.reflect` | The `thorns` graph's `Execute Cue`, once per reflected hit | `AudioCueHandler` with `reflect.ogg` |

The animations are transform tracks and nothing more: `hit` squashes the body, `shoot` kicks it back, `attack` lunges and leans it forward. Each scene keeps them in an `AnimationPlayer` named `FxPlayer`, separate from the player's roll, because `Play Animation` ends the moment another clip replaces its own — a recoil on the dash's player would cut the roll short. Every clip keys all of the properties the others move, so one interrupting another leaves nothing stuck mid-pose. The hit handler has `Interrupt` off, so a flinch yields to a clip already playing: thorns reflects inside the enemy's own swing, and without that the lunge would be replaced by the squash on the frame it started.

What is not a cue is a graph node. `Play Audio One Shot`s drive players the scenes carry: the shot and the dash (`ShotSfx`, `DashSfx` on the player), the absorbed hit (`ShieldHitSfx`, in `shield`), the fall (`FallSfx`, in `respawn`), the swing (`SwingSfx` on the enemy, in `enemy_attack`) and the brazier's click (`ToggleSfx` on the brazier, in `brazier`). The death burst and the ember flight are `Scene` nodes in `enemy_brain` and `brazier`, and the sounds of those moments — the death, the ember leaving and landing — are autoplay players inside the VFX scenes themselves, so they play wherever the scene is put and outlive the thing that spawned them. The `AudioCueHandler`s create a positional player on whoever the cue lands on, so a burning projectile crackles as it flies. `fizzle.ogg`, the dry click, is the controller's refused-press sound.
