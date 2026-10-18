# Demo 00 — Quick Start

The finished result of the [Quick Start guide](../../../docs/quick-start.md). Open it next to the guide to compare it with what you built.

Arrow keys move. Walk into the campfire to burn and out of it to stop. Space heals 30 Health for 25 Mana, with a 2 second cooldown, and Mana comes back on its own.

## What is in it

| Piece | Where | What it is |
| --- | --- | --- |
| `HeroAttributes` | `attributes/hero_attributes.tres` | An attribute set definition with Health and Mana. The class it generates is in `res://forge_generated/attribute_sets/`. |
| Player | `scenes/player.tscn` | A `CharacterBody2D` with a `ForgeEntity`, the `HeroAttributes` set, and two `ForgeEffect` children applied when it is ready: `Mana Regen` and `Grant Heal`. |
| Campfire | `scenes/quick_start.tscn` | An `EffectArea2D` in `OnStay` mode holding `effects/burning.tres`: 5 damage every half second while you stand in it, removed when you leave. |
| Heal | `abilities/heal.tres`, `abilities/heal_graph.tres` | An ability whose behavior is a four-node Statescript graph: wait for Space, commit the cost and cooldown, apply `effects/heal.tres`. `Grant Heal` grants it with **Try Activate On Grant**, so the graph starts listening at once. |
| Cues | `Cues` in `scenes/quick_start.tscn` | Library cue handlers, no scripts. `cue.burn` starts the burn particles, flashes the body red each tick and plays the fire loop; `cue.heal` bursts the heal particles and plays the heal sound. |

## The only C# here

| Script | Job |
| --- | --- |
| `QuickStartPlayer` | Arrow-key movement. It never touches Forge. |
| `AttributeBar` | A `ProgressBar` that follows one attribute of a `ForgeEntity`. The Health and Mana bars above the player are two of them. |
