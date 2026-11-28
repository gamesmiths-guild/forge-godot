# Demo 06 — Networked Arena

A first-person deathmatch for up to four players, built on Forge's [shots](../../../docs/shots.md), with every ability a C# behavior rather than a Statescript graph. Five weapons are out from the start: a pistol, a rifle, a shotgun, a rocket launcher and a shock rifle. Dummies strafe across a target range on the north side, so one player can try every weapon on moving targets.

WASD moves, the mouse looks, Space jumps, left click fires, and right click fires the shock rifle's ball. 1 to 5 take out a weapon, and the mouse wheel goes through them. Tab shows the scoreboard. Click to capture the mouse, and Esc frees it. **F3** shows the session's numbers, and **F4** makes the link worse, from perfect to good, far and poor.

## Running it

The level opens on the same menu as [Demo 05](../05_networked_statescript/README.md):

| Button | What it does |
| --- | --- |
| **Host** | Hosts on port 7777 with a player on this machine. Nobody has to join: alone, it's the target range. |
| **Join** | Joins the address typed above it. When the connection fails or ends, the menu comes back on a fresh level. |
| **Dedicated** | Hosts with nobody playing on this machine. |

To try it on one computer, open **Debug, Customize Run Instances**, enable multiple instances, and run two or more. The level also reads `--host`, `--join` (or `--join=192.168.1.10`) and `--dedicated` after Godot's own `--`, so each instance can skip the menu.

## The weapons

| Weapon | Fire | Alt fire |
| --- | --- | --- |
| **Pistol** (1) | A hitscan bullet, 120 damage, every 0.3 s. It goes through a crate. | |
| **Rifle** (2) | A burst of three hitscan bullets of 100, 0.09 s apart, every 0.45 s after the last. Each one kicks your view up, so the burst climbs unless you pull it down. They go through a crate. | |
| **Shotgun** (3) | Eight hitscan pellets of 70 in a 5° cone, weaker past 8 units. They go through a crate. | |
| **Rocket launcher** (4) | A rocket whose blast deals up to 500 and pushes everyone in 4 units, its shooter included, at a third of the damage, so a rocket at your feet jumps you. | |
| **Shock rifle** (5) | A hitscan beam, 340 damage, every 0.75 s. It goes through a crate. | A slow shock ball, 250 damage on a hit. Hit a ball with the beam and it bursts, 650 damage and a push across 4 units. |

A hit on a head deals double. Health is 1000, and nothing regenerates: a player whose health runs out is fragged, waits 2.5 s out of the fight and respawns at the spawn point furthest from the other players, at full health. A dummy bursts and is back at full health half a second later.

## What to look for

- **Your shots answer at once.** Your client runs your player ahead of the server, its weapons included: the bullets, the beam, their tracers and sparks, the rocket and the pellets leave the moment you click, and the server's word that they did never shows them twice. Press F4 a few times and they still do.
- **Your hits land as you saw them.** The server tests your shot against the world as your client showed it to you, a moment behind: a dummy strafing across your sights is where you saw it, and so is its head.
- **Heads lean.** Each character's head and torso are hitboxes, posed by an animation tree that leans them into a strafe. The tree runs on the server too, a dedicated one included, and the server records where each hitbox stood every tick, so a shot that rewinds meets the head where the shooter's screen leaned it.
- **Crates and walls.** A crate is wood: its collider has a shot surface, so the bullets, the beam and the pellets go through it, keeping what its thickness leaves them, about 0.4 of their damage. Rockets and shock balls have no power and stop at it. The stone walls have no surface and stop everything.
- **Recoil.** Each of the rifle's shots executes a recoil cue, which kicks your view up at once and settles back. Your view aims your input, so the kick moves the burst's next shots, and the server fires them where your input says, as your screen showed them.
- **Rocket jumps.** A blast pushes the bodies it reaches, its shooter's too. Your client predicts your own push with the rest of your player, and the movement keeps its speed in the air, falling at 1.8 times the project's gravity for a quick jump, so a rocket at your feet throws you over a wall.
- **The combo.** The shock ball can be shot: the beam tests it where your client showed it, and the ball bursts where they met, on every screen.
- **Everyone else's shots show where they hit.** A tracer, a spark or a frag's burst is a cue the server executes with the point it happened at, which travels with the cue: every screen draws another player's beam to where the server's beam stopped.
- **The score is the server's.** Kills and deaths are attributes of each player's entity, so every scoreboard reads the server's numbers. The kill feed is a cue on the fragged entity whose source is the fragger, and whose magnitude is the weapon.

## How it's built

Every ability is a C# `ForgeAbilityBehavior` resource with a `RollbackBehavior` inside, whose whole run lives in its context, so a client predicts it and a correction runs it again exactly. Each one changes the game only through effects, tags, attributes and events, and shows things only through cues.

| Ability | Behavior | What it does |
| --- | --- | --- |
| Pistol, Rifle, Shotgun, Rocket Launcher, Shock Rifle | `HoldWeapon` | Runs until another weapon's ability cancels it, so its activation owned tag says which weapon is out. Each cancels the abilities tagged `weapon`, which puts the held weapon away and stops its fire. |
| Next Weapon, Previous Weapon | `CycleWeapon` | Activates the weapon after or before the one held, and ends. |
| Pistol Shot, Rifle Burst, Shotgun Blast, Shock Beam | `HitscanWeapon` | Fires a hitscan per pellet along directions spread from the entity's seeded random, and executes a tracer cue to where each stopped and an impact cue on the surface it hit. |
| Rocket, Shock Ball | `ProjectileWeapon` | Launches a lightweight projectile, whose resource holds its damage, its blast and what shows it. It starts short of a wall in front of the eye, since a shot's sweep leaves out what it starts inside. |
| Die, Dummy Die | `Die` | Server Only, triggered by the event the arena's damage raises on a lethal hit, naming the fragger. It executes the frag cue, counts a death and a kill, sends a player to wait under the arena, and respawns it. Its activation owned tags keep the entity dead, immune and still meanwhile. |

The fire abilities share `WeaponFire`: a burst as the ability starts, one shot unless the weapon says more, then another burst every refire while the button is held or was pressed again, then the end. Each shot executes the weapon's shot cue and, for the rifle, its recoil cue. Their activation requires the weapon's tag, and the wait for the next shot and the shots left in the burst are in the context, so a client that runs a tick again fires on the same tick as before.

The arena's damage is its own execution, `ArenaDamageExecution`: the effect's damage times the strength the shot hit with, which a headshot doubled and a crate cut. When it takes the last of a target's health it raises `event.arena.frag` with the fragger, which the Die ability takes as its typed payload.

## Layout

| Path | Contents |
| --- | --- |
| `scenes/networked_arena.tscn` | The arena, the target range, the session, the menu, the HUD and the cue handlers. |
| `scenes/player.tscn` | The networked first-person player: movement relative to the aim, jump and air control, the camera, the weapon models, and its hitboxes. |
| `scenes/dummy.tscn` | A target dummy that strafes, with the same hitboxes, placed in the level and networked there. |
| `scenes/crate.tscn` | A wooden crate, with its shot surface. |
| `scenes/vfx/` | The projectiles in flight, their impacts and the impact spark. |
| `abilities/` | The weapons, the weapon switch, the fire abilities, the frags, and the grants. |
| `effects/` | Each weapon's damage, the kill and death counts, and the respawn's heal. |
| `shots/` | The hitscans, the projectiles, the blasts and the crates' wood. |
| `scripts/abilities/` | The abilities' behaviors. |
| `scripts/ArenaGame.cs` | The menu and the arguments, a numbered player per peer, and the mouse. |
| `scripts/ArenaHud.cs` | Health, the weapon held, who fragged you, and the scoreboard. |
| `scripts/FirstPersonView.cs` | The mouse look, the recoil's kick and the camera on this machine's player, which the input aims from. |
| `scripts/HeldWeaponModel.cs` | Shows the weapon an entity holds, from its tags. |
| `scripts/StrafeLean.cs` | Leans a character's hitboxes into its strafe, from its body's velocity. |
| `scripts/StrafingDummy3D.cs` | Moves a dummy along its line where it's simulated. |
| `scripts/RecoilCueHandler.cs` | Kicks the shooter's view up for the recoil cue, on the machine whose player it is. |
| `scripts/TracerCueHandler3D.cs` | Draws a tracer from the shooter's muzzle to a cue's point. |
| `scripts/KillFeedCueHandler.cs` | The kill feed, from the frag cue. |
| `scripts/ArenaAttributes.cs` | A player's number, kills and deaths. |
| `scripts/ArenaDamageExecution.cs` | The arena's damage, and the frag event. |

`network_manifest.tres` is the demo's own manifest: the player's scene, the effects that can be on its entities, the abilities and the two projectiles. Every machine must run the same build.
