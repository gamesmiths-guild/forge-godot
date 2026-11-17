# Demo 05 — Networked Statescript

The Statescript demo, played over the network by up to four players: the same level, the same four player abilities, the same enemy and the same HUD, with every ability still a Statescript graph. One machine hosts, as a player or as a dedicated server, and the others join it by address. The server simulates every entity from each player's input and sends the world down; every other machine shows it as the server says it is, a moment behind, and runs its own player ahead, predicted.

It plays as [Demo 03](../03_statescript/README.md) does, with two differences on purpose: a dashing player keeps colliding with enemies and other players, so two players can dash into each other, and shots hit other players too.

Movement is WASD or the arrow keys, and aim is the mouse. Left click shoots, Space dashes, 3 raises and lowers the shield, and E inside the brazier's ring lights it or puts it out. **F3** shows the session's numbers, **F4** makes the link worse, from perfect to good, far and poor, and **F5** prints the tags this machine sent.

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

- **Your own player answers at once.** Your client predicts it: it runs your player ahead of the server from your keys, and checks each tick against what the server sends. Press F4 a few times for 200 ms each way and it still walks, shoots, dashes and shields the moment you press, with the mana spent and the cooldown started on the action bar; the overlay counts the ticks the server confirmed and the ones it corrected. A correction comes from what your client couldn't know: a hit, the fire's burns, the water, a push from an enemy, another player in the way. It eases out on the player's model rather than jumping.
- **Dash into another player.** Players collide, the dash too: the sample's dash has no Collision Override. Your client shows the other player a moment behind, so the dash may stop a little short or long of where the server stops it, and the correction eases out.
- **Your shots leave at once.** Your client makes a shot the moment you click, ahead of the server, and the server's shot, arriving a moment later, takes over the one your client made instead of making another. It flies like the Statescript demo's projectile: through the world, 20 units at 10 a second, until it meets a dummy, an enemy or another player. The hit is the server's: it deals the damage, less the further the shot flew, and frees the shot. Your client frees its own where it sees the hit.
- **Your hits land as you see them.** The server tests your shot against the world as your client showed it to you, a moment behind: every other entity's body stands where you saw it while your shot flies, so a shot that hit a moving enemy on your screen hits it on the server too. The compensation fades out over the shot's first second of flight, so a slow shot from across the level meets its target nearer where it really is. The flash, the sound and the hit animation play on your screen the moment your shot meets the target, without waiting for the server; the damage number waits for what the server dealt.
- **Fire spreads by a hit.** Shoot through the bonfire and the shot catches fire a moment after it passes, once the server's word that it did arrives, and sets whatever it hits burning. A burning enemy that hits you sets you burning too. The water puts it out, with a hiss, and leaves you wet.
- **The shield.** It's a predicted toggle: the effect, the bubble and its hum start as you press. What only the server knows reaches you as it happens: each hit absorbed plays its sound and costs its mana, and the shield drops when the mana runs out.
- **Thorns.** An enemy's hit is the server's, so the reflection it triggers is too: the ability is Server Only. The burst, the shake and the cooldown on the action bar arrive from the server.
- **Everyone else moves smoothly** between the states the server sends, 32 a second, shown as little behind as the link allows: the session's interpolation is Adaptive, about 40 ms behind on a steady link and more as packets come late or get lost. F3 shows how far. A respawn is a jump, not a slide: two states further apart than the session's Interpolation Snap Distance aren't eased between.
- **Effects show everywhere.** Walk through the bonfire and you burn on every screen: the flames are the fire effect's persistent cue, the burn flashes and the numbers are its executed cues, and the health bar follows the server's health. The regeneration pad heals the same way.
- **Everyone's graphs show everywhere.** Another player's dash rolls and whooshes on your screen, and an enemy lunges and swings: their graphs run on the server, and your copy of each plays its animation and sound as the server says, without running the ability. An enemy whose health runs out bursts on every screen as it goes.
- **The brazier.** E inside its ring lights it for everyone, whoever else is in it: the flame and the light are its graph's Node Property Overrides, held on every screen while the server's are. While it's lit and fewer than six enemies are alive, it sends an ember to a spawn point beyond the chasm every 5 s, and an enemy follows it out of a burst and comes over the bridge for the nearest player. The ember's flight is a Move To on a cosmetic scene, so every machine flies its own. The light's flicker is a client-only region of its graph, which every machine runs by itself, and the enemy's spawn a server-only one, since the server spawns it for everyone.
- **Everyone's effects are over their heads.** The label above a player or an enemy lists the effects on it, with stacks and time left, read from its entity's `EffectsManager`. On a client those are the server's effects as the mirror holds them: they count down with the server's ticks, and run nothing.
- **Not everyone hears of everything.** Dash and your cooldown shows over your head, on your screen alone: it's Owner Only, so the others' mirrors of you don't have it. The effects that grant abilities are Server Only, so nobody's label shows them, a listen host's included.
- **Join late.** A client that joins mid-fight gets the world as it is: the enemies where they are, who's burning, the dummies' health, and a lit brazier lit.

## What's networked

| Entity | How it gets to a client |
| --- | --- |
| Players | Spawned by the server for each peer that joins, owned by that peer, from `scenes/player.tscn`. |
| Shots | Spawned by the graph of the player who fires, from `scenes/projectile.tscn`: by the server for everyone, and ahead of it by the client of the player who fired, which the server's spawn binds. |
| Enemies | Spawned by the brazier's graph on the server, from `scenes/enemy.tscn`. |
| Dummies | Placed in the level on every machine, networked through an override on the instances, so the server's regeneration is what every client sees. |
| Brazier | Placed in the level, networked through an override on the instance, with its own grant of the sample's brazier ability, which the server runs. |

The stations' areas are on every machine, but a client's copies of entities hold what the server sends and take no effects of their own, so only the server's areas count.

`network_manifest.tres` is the sample's own manifest, listing the three scenes the server spawns, the effects that can be on its entities and the abilities whose graphs clients show, so the sample works whatever the project's manifest holds. Every machine must run the same build.

## What differs from the Statescript demo, and why

Everything not listed here is the Statescript demo's own, referenced in place.

| Here | There | Why |
| --- | --- | --- |
| `ForgeCharacterMovement3D` and `ForgeInputCollector` on the player | `PlayerController3D` | The server and the player's client both move the player from the same input, tick by tick, so movement can be predicted and corrected. The speeds, acceleration and facing are the same. |
| Abilities bound to input actions (`skill_1`, `skill_2`, `skill_3`) | Activated by the controller | A press travels as input, and both machines activate from it on the same tick. |
| `PlayerHud` | The controller's HUD mirror | It reads this machine's player, whichever entity the session names. |
| The shot is an entity flown by `graphs/projectile_flight.tres` | `ForgeProjectile3D` | A graph's flight rolls back and runs again with the player; the projectile node keeps its flight in its own fields. Its launch point, for the falloff, sits in a server-only region. |
| The shot's entity has Compensate Queries on, fading over 1000 ms | None | The server tests it as its shooter saw the world. A shot this slow takes a second to cross ten units, so the default 250 ms would leave all but the nearest hits uncompensated. |
| `abilities/shot_damage.tres`, with the damage number's cue apart and Never predicted | The shared `damage.tres` | The shooter's client shows the flash and the sound at once, and the number waits for the server. |
| `graphs/enemy_brain.tres` chases and attacks the nearest of the `player` group | `%Player` | There is more than one player. |
| `ServerPuppetBody3D` on the enemy | `PuppetBody3D` | A client's copy of an enemy is placed by the session, so it mustn't move itself. |
| `graphs/brazier.tres` reads the input of everyone in its ring, and frames its enemy spawn and its flicker in regions | One player's key | Whoever is in the ring may press, the spawn is the server's alone, and the flicker is each machine's own. |
| Thorns is Server Only | Predictable | It reads the enemies around the player, which a client shows a moment behind. |
| `graphs/dash.tres` has no Collision Override | Passes through enemies | One of the two differences in how it plays. |
| `ShotBody3D` on the shot, which meets players too | Meets dummies and enemies | The other: players can shoot each other. It passes through the player who fired it. |
| The effects label over each character | None | It shows what each machine hears of an entity's effects. |
| The session's interpolation is Adaptive | None | Shows the other entities as fresh as the link allows, so hits and moves read sooner. |

## Layout

| Path | Contents |
| --- | --- |
| `scenes/networked_level.tscn` | The Statescript demo's level without its player, plus the session, the overlay, the menu, the HUD and the game. |
| `scenes/player.tscn` | The Statescript demo's player, networked, moved by a movement component, with its model under the `Visual` the component eases after a correction. |
| `scenes/projectile.tscn` | The shot: a networked, flammable body on the projectiles layer that meets enemies and players. |
| `scenes/enemy.tscn` | The Statescript demo's enemy, inherited, networked, with the sample's grants and puppet body. |
| `abilities/` | The players', the shots', the enemies' and the brazier's grants, all Server Only, and the sample's own ability data: the shot, its flight and its damage, the dash, whose cooldown is Owner Only, the shield, thorns, the enemy brain and the brazier. |
| `graphs/` | The sample's own graphs: the brazier, the dash, the shot and its flight, and the enemy brain. The shield, thorns, respawn and enemy attack graphs are the Statescript demo's. |
| `scripts/NetworkedLevel.cs` | The menu and the arguments, a player per peer, and the camera on this machine's player. |
| `scripts/PlayerHud.cs` | The action bar, the tags and the refused-press click for this machine's player. |
| `scripts/ServerPuppetBody3D.cs` | The enemy's body, moving only where the enemy is simulated. |
| `scripts/ShotBody3D.cs` | The shot's body, which passes through the player who fired it. |
| `scripts/EffectsLabel.cs` | The effects on an entity, above it, as this machine's player would hear of them. |
