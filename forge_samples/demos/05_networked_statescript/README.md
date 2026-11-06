# Demo 05 — Networked Statescript

The Statescript demo's level, played over the network by up to four players. One machine hosts, as a player or as a dedicated server, and the others join it by address. The server simulates every entity from each player's input and sends the world down; every other machine shows it as the server says it is, a moment behind.

Movement is WASD or the arrow keys, and Space dashes toward the mouse. **F3** shows the session's numbers, and **F4** makes the link worse, from perfect to good, far and poor.

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

- **Your own player answers late.** Nothing is predicted yet: your keys go to the server, the server moves your player, and you see it where the server had it a moment ago, like everyone else. On localhost it's barely visible; press F4 a few times for 200 ms each way and it is.
- **Everyone else moves smoothly** between the states the server sends, about 30 a second, shown 100 ms behind.
- **Effects show everywhere.** Walk through the bonfire and you burn on every screen: the flames are the fire effect's persistent cue, the burn flashes and the numbers are its executed cues, and the health bar follows the server's health. The lake puts it out with a hiss, and the regeneration pad heals, the same way.
- **Enemies come for the nearest player.** The server keeps four on the plateau. They burn like anyone, and one whose health runs out or that falls off the edge is gone from every screen.
- **Join late.** A client that joins mid-fight gets the world as it is: the enemies where they are, who's burning, the dummies' health.

## What's networked

| Entity | How it gets to a client |
| --- | --- |
| Players | Spawned by the server for each peer that joins, owned by that peer, from `scenes/player.tscn`. |
| Enemies | Spawned by the server at the `enemy_spawns` markers, from `scenes/enemy.tscn`. |
| Dummies | Placed in the level on every machine, networked through an override on the instances, so the server's regeneration is what every client sees. |

The stations' areas are on every machine, but a client's copies of entities hold what the server sends and take no effects of their own, so only the server's areas count.

`network_manifest.tres` is the sample's own manifest, listing the two scenes the server spawns, so the sample works whatever the project's manifest holds. Every machine must run the same build.

## Layout

| Path | Contents |
| --- | --- |
| `scenes/networked_level.tscn` | Sample 03's level without the brazier, its player and its HUD, plus the session, the overlay, the menu and the game. |
| `scenes/player.tscn` | Sample 04's player, networked, with health and a health bar. |
| `scenes/enemy.tscn` | Sample 04's enemy, networked, with `EnemyBrain` instead of its chase. |
| `abilities/` | The players' grants (the dash, the fire spreading and the fall respawn) and the enemies' (the fire spreading). |
| `scripts/NetworkedLevel.cs` | The menu and the arguments, a player per peer, the enemies, and the camera on this machine's player. |
| `scripts/EnemyBrain.cs` | On the server, walks an enemy toward the nearest player through its input, and frees it once it's dead or has fallen. |

Everything else is referenced in place from the Statescript and Rewind demos and the shared folder.
