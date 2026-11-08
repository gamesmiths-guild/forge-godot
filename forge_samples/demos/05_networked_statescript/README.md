# Demo 05 — Networked Statescript

The Statescript demo's level, played over the network by up to four players. One machine hosts, as a player or as a dedicated server, and the others join it by address. The server simulates every entity from each player's input and sends the world down; every other machine shows it as the server says it is, a moment behind, and runs its own player ahead, predicted.

Movement is WASD or the arrow keys, Space dashes toward the mouse, and E inside the brazier's ring lights it or puts it out. **F3** shows the session's numbers, and **F4** makes the link worse, from perfect to good, far and poor.

## Running it

The level opens on a menu:

| Button | What it does |
| --- | --- |
| **Host** | Hosts on port 7777 with a player on this machine. Nobody has to join: alone, it's the single-player game. |
| **Join** | Joins the address typed above it. When the connection fails or ends, the menu comes back on a fresh level, since joining makes the level's dummies mirrors for good. |
| **Dedicated** | Hosts with nobody playing on this machine, which shows the world and presents nothing. |

To try it on one computer, open **Debug, Customize Run Instances**, enable multiple instances, and run two or more. The level also reads arguments after Godot's own `--`, so each instance can skip the menu: `--host`, `--join` (or `--join=192.168.1.10`) and `--dedicated`. A dedicated server runs headless too:

```bash
godot --headless --path . res://forge_samples/demos/05_networked_statescript/scenes/networked_level.tscn -- --dedicated
```

## What to look for

- **Your own player answers at once.** Your client predicts it: it runs your player ahead of the server from your keys, and checks each tick against what the server sends. Press F4 a few times for 200 ms each way and it still walks and dashes the moment you press; the overlay counts the ticks the server confirmed and the ones it corrected. A correction comes from what your client couldn't know: the fire's burns, a push from an enemy, another player in the way. It eases out on the player's model rather than jumping.
- **Dash into another player.** Players collide, the dash too: the sample's dash has no Collision Override. Your client shows the other player a moment behind, so the dash may stop a little short or long of where the server stops it, and the correction eases out.
- **Fire spreads from the server.** Burning spreads to whoever stands near, which only the server sees right, so the sample's Spread Fire is Server Only: your client shows it, and doesn't predict it.
- **Everyone else moves smoothly** between the states the server sends, about 30 a second, shown 100 ms behind.
- **Effects show everywhere.** Walk through the bonfire and you burn on every screen: the flames are the fire effect's persistent cue, the burn flashes and the numbers are its executed cues, and the health bar follows the server's health. The lake puts it out with a hiss, and the regeneration pad heals, the same way.
- **Everyone's graphs show everywhere.** Another player's dash rolls and whooshes on your screen: their graph runs on the server, and your copy of it plays its animation and sound as the server says, without running the dash. An enemy whose health runs out bursts on every screen as it goes.
- **The brazier.** E inside its ring lights it for everyone: the flame and the light are its graph's Node Property Overrides, held on every screen while the server's are. While it's lit it sends an ember to a spawn point every 5 s, and an enemy follows it out of a burst. The ember's flight is a Move To on a cosmetic scene, so every machine flies its own. The light's flicker is a client-only region of its graph, which every machine runs by itself, and the enemy's spawn a server-only one, since the server spawns it for everyone.
- **Everyone's effects are over their heads.** The label above a player or an enemy lists the effects on it, with stacks and time left, read from its entity's `EffectsManager`. On a client those are the server's effects as the mirror holds them: they count down with the server's ticks, and run nothing.
- **Not everyone hears of everything.** Dash and your cooldown shows over your head, on your screen alone: it's Owner Only, so the others' mirrors of you don't have it. The effects that grant abilities are Server Only, so nobody's label shows them, a listen host's included.
- **Enemies come for the nearest player.** The server keeps four on the plateau. They burn like anyone, and one whose health runs out or that falls off the edge is gone from every screen.
- **Join late.** A client that joins mid-fight gets the world as it is: the enemies where they are, who's burning, the dummies' health, and a lit brazier lit.

## What's networked

| Entity | How it gets to a client |
| --- | --- |
| Players | Spawned by the server for each peer that joins, owned by that peer, from `scenes/player.tscn`. |
| Enemies | Spawned by the server at the `enemy_spawns` markers, from `scenes/enemy.tscn`. |
| Dummies | Placed in the level on every machine, networked through an override on the instances, so the server's regeneration is what every client sees. |
| Brazier | Placed in the level, networked through an override on the instance, with its own grant of the sample's brazier ability, which the server runs. |

The stations' areas are on every machine, but a client's copies of entities hold what the server sends and take no effects of their own, so only the server's areas count.

`network_manifest.tres` is the sample's own manifest, listing the two scenes the server spawns, the effects that can be on its entities and the abilities whose graphs clients show, so the sample works whatever the project's manifest holds. Every machine must run the same build.

## Layout

| Path | Contents |
| --- | --- |
| `scenes/networked_level.tscn` | Sample 03's level without its player and its HUD, plus the session, the overlay, the menu and the game. |
| `scenes/player.tscn` | Sample 04's player, networked, with health, a health bar and a label of its effects, its model under the `Visual` its movement component eases after a correction. |
| `scenes/enemy.tscn` | Sample 04's enemy, networked, with `EnemyBrain` instead of its chase, and the same bar and label. |
| `abilities/` | The players' grants (the dash, the fire spreading and the fall respawn), the enemies' (the fire spreading and their death) and the brazier's, all Server Only, the sample's own dash, whose cooldown is Owner Only, and its own Spread Fire, a Server Only ability. |
| `graphs/` | Sample 03's brazier, reading the input of the player in its ring and spawning the sample's enemy, with its regions, sample 04's enemy death without its spawner, and sample 03's dash without its Collision Override, so a dash stops at another player. |
| `scripts/NetworkedLevel.cs` | The menu and the arguments, a player per peer, the enemies, and the camera on this machine's player. |
| `scripts/EffectsLabel.cs` | The effects on an entity, above it, as this machine's player would hear of them. |
| `scripts/EnemyBrain.cs` | On the server, walks an enemy toward the nearest player through its input, and frees it once it has fallen. |

Everything else is referenced in place from the Statescript and Rewind demos and the shared folder.
