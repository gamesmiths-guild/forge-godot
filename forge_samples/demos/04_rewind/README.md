# Demo 04 — Rewind

A small walled arena where every bit of gameplay state lives in Forge, so a `ForgeWorldHistory` can pause the fight, roll the whole arena back up to five seconds, and play on from any moment. Catch fire at the bonfire, carry it to the enemies and watch them burn, then go back to before the fire spread and before they died.

Movement is WASD or the arrow keys, left click shoots, Space dashes toward the mouse, Backspace or right click pauses, R replays from the paused moment, and Esc goes back to the hub.

## Rewinding

Backspace or right click pauses the game and shows the timeline at the bottom of the screen. Drag the needle, or press the arrow keys, to show any of the last five seconds. Nothing is simulated while you scrub: every entity is put back as that physics step left it, an enemy that died since is back, and the one that replaced it is gone.

Pressing it again resumes from the step shown, and the steps after it are left behind for good: the enemy that died in them lives on, its replacement was never spawned, and a shot fired in them was never fired. Scrubbing forward again before resuming brings it all back.

What you see follows the needle: an enemy's flames come and go with the fire cue of its burning, the dash's roll shows the pose of the step shown, and sounds stay quiet while paused, then play on from the right place.

## Replaying

With the game paused, R plays the steps after the needle again, from the recording and with the input each entity had, and checks every step against the one recorded: each entity's state is hashed, its body to a thousandth of a unit, and the two hashes compared. The needle moves on as the replay goes, and the replay stops at the present and pauses there. R or Backspace stops it early, where it is.

A step that came out differently is marked red on the timeline. Put the needle on a mark to see, under the timeline, each value that differed, as recorded and as replayed. The sample is meant to replay without a mark from any step: one would mean something in it ran outside Forge's simulation.

The corner readout shows what the history costs: how many entity snapshots it holds, about how much memory they take (what recording the last step allocated, times the steps held), and how long the last step took to capture.

## What's in the level

| Node | What it is |
| --- | --- |
| `World History` | A `ForgeWorldHistory` under the level root. It advances every `ForgeEntity` in the level each physics step and records the step, keeping five seconds of them. |
| `Rewind` | `RewindTimeline`, a `CanvasLayer` that keeps processing while the tree is paused. The `rewind` action (Backspace or right click) pauses and unpauses the tree, and the slider calls `Seek`. The `replay` action (R) turns on the world history's `Replaying` and unpauses, marks each step its `Desynced` event reports, and pauses again at the present. Leaving the level unpauses it, so Esc from the timeline doesn't leave the hub paused. |
| `Player` | A `ForgeCharacterMovement3D` walks it from a `ForgeInputCollector`'s input. The dash is the Statescript demo's graph, bound to Space through the ability's input action, and left click fires a projectile. It's flammable but has no health, so fire on the player only spreads. |
| `Spawned` | Holds the three enemies the level starts with, and every enemy and projectile spawned later. Each enemy walks with the same movement component, steered by `ChaseInput`, and has 100 health. |
| `Bonfire` | The Statescript demo's bonfire rebuilt as an entity: the same logs, flame, light and crackle, and a sign in the same style, without its effect area. It's always burning: its grant gives it Spread Fire, then the `effect.fire` tag that wakes it. |
| `Spawner` | An entity whose Respawn Enemy ability answers each `event.death` event with an instance of its own. |
| `SpawnPoints` | Four `Marker3D`s in the `spawn_points` group, in the arena's corners. |
| `FireVFX Cues` | The Statescript demo's fire handler: while `cue.vfx.fire` is applied to a body, its fire particles burn on it. |
| `FloatingText Cues`, `BurnFlash Cues`, `HitFlash Cues`, `Burn SFX`, `Hit SFX` | The Statescript demo's handlers for each burn tick and projectile hit: the damage number, the orange or white flash, and the hit sound, quieter for a burn. |
| `QuitScene` | The demos' Esc handler, set to keep processing while the tree is paused, so Esc goes back to the hub from the timeline too. |

## The graphs

| Graph | Who runs it | What it does |
| --- | --- | --- |
| `spread_fire` | Anything burning, triggered while `effect.fire` is present | An Overlap 3D node watches a sphere of 1.4 units around it every tick and applies `effects/burning.tres` to each entity that enters it, nearest first. For as long as it runs, a Play Audio plays `FireSfx`, the fire loop (the bonfire's is its crackle). |
| `enemy_death` | Enemy, triggered by `state.awake` | An Attribute Listener on Health. At zero it instantiates the Statescript demo's death burst, raises `event.death` on `%Spawner`, and frees the enemy with Queue Free, which sends it to limbo. |
| `respawn_enemy` | Spawner, triggered by `event.death` | Waits 0.3 seconds, picks a random marker in `spawn_points` and a spot up to a unit from it each way, and instantiates the Statescript demo's spawn burst and a new enemy there, under `%Spawned`. The ability runs an instance per death, so enemies that die together all come back. |
| `projectile` | Player, bound to left click (`skill_1`) | Commits the Statescript demo's projectile cooldown, instantiates `projectile.tscn` under `%Spawned` in front of the player and facing where it faces, and plays the shot sound and the recoil. |
| `projectile_flight` | Projectile, triggered by `state.awake` | A Move Body 3D flies it 16 units ahead at 18 units a second and stops it at the first wall or enemy in the way. An enemy takes `effects/projectile_hit.tres`. Either way Queue Free then sends the projectile to limbo. |

`effects/burning.tres` lasts six seconds and takes 10 health every half second, so an enemy burns to death in five. It needs `trait.flammable` and doesn't apply to anything already burning, so standing in a fire doesn't stack it. For as long as it lasts it applies `cue.vfx.fire`, the persistent cue the Statescript demo shows its fire with. Each tick that took health executes the Statescript demo's burn cues: `cue.floating.text`, `cue.vfx.burn` and `cue.sfx.burn`. The player has no health to take, so fire on the player shows no numbers or flashes.

`effects/projectile_hit.tres` takes 10 health at once and executes the Statescript demo's hit cues: `cue.floating.text`, `cue.vfx.hit` and `cue.sfx.hit`.

The spawner scatters each enemy around its marker because Godot Physics can't separate two bodies put in exactly the same place: each step pushes both up out of the other, each rides the other, and their speed grows until the physics server reports them gone.

## Why it rewinds cleanly

The sample is strict: everything that decides what happens next is Forge state, which is what the world history can bring back.

- **Bodies** belong to a `ForgeCharacterMovement3D`, the entities' mover, so their positions and velocities are part of every snapshot. It walks them, and the only other thing that moves one is a Move Body 3D in a graph, which steps on the tick and keeps its progress in the graph: the dash, and a projectile's flight. A projectile's movement component never walks it, since its flight blocks it, but it's what puts the projectile's body in the snapshots. Nothing else moves them: no Nav Move To, no body script with a physics step of its own, no `ForgeProjectile3D`, which flies itself.
- **Contact** is a physics query a graph makes: Overlap 3D polls the physics server from its graph and keeps who is inside in its node context, which every snapshot copies. An `EffectArea3D` keeps its own list of who is inside, which a rollback can't reach.
- **The enemies' minds are input.** `ChaseInput` turns the player's position into a move vector each step, the same kind of frame a keyboard makes, and the history keeps it like a player's.
- **Randomness** comes from each entity's `DeterministicRandom`, which every snapshot includes, so the spawner draws its spawn points from rolled-back state.
- **Deaths and spawns** go through Queue Free and Instantiate Scene, which the world history turns into limbo and new members of the world. A projectile is one too, from the shot to the hit.
- **The screen follows the state.** The health bars read Health every frame, since a rollback raises no change event. The fire on a burning body is the burning effect's persistent cue, which a rollback ends and starts again with the effect. The fire's sound and the dash's roll are a Play Audio and a Play Animation, which the presentation pass shows as the graph stands after a rollback, at the point of the activation the step shown gives them.
- **Spawns roll the same way twice.** The world history seeds a spawned enemy or projectile from the step and its place among the step's spawns, so one a replay spawns again rolls as the first one did.

The capture and restore hook for state outside Forge, `IForgeEntity.StateHook`, exists for what can't live in Forge. Nothing here needs it.

## What doesn't rewind

- The death and spawn bursts and their sounds are cosmetic scenes, and so are the damage numbers. One already playing keeps playing through a rollback. A replay spawns them again as it runs the steps.

## Layout

| Path | Contents |
| --- | --- |
| `abilities/` | `ForgeAbilityData` for the dash, Projectile, Projectile Flight, Spread Fire, Enemy Death and Respawn Enemy, and the grant effect of each entity type. |
| `effects/` | `burning.tres` and `projectile_hit.tres`. |
| `graphs/` | The five `StatescriptGraph` resources. |
| `scenes/` | `rewind_level.tscn`, `player.tscn`, `enemy.tscn`, `projectile.tscn` and `bonfire.tscn`. |
| `scripts/` | `RewindTimeline`, `ChaseInput` and `LiveHealthBar`, the only C# in the demo. |

The look is the Statescript demo's, referenced as it is: the materials, the bonfire's parts, the trees, the fire particles and the health bar (both from the Real-Time 3D demo), the fire, burn and hit cue handlers, the death and spawn bursts, the dash graph and cooldown, and the projectile's look, cooldown, shot sound and recoil.
