# Demo 02 — Turn-Based 2D

The Real-Time 3D demo's effects — fire, water, damage, heal and regeneration — on a 2D board where **time only moves when you do**. Each key press is one turn, and one turn is one second of Forge time, so a 10 s fire is ten moves of burning and Rejuvenation ticks once per step. There are no abilities and no enemies; the point is to watch durations, periods and stacks advance under your control.

WASD or the arrow keys step 20 px in that direction. Space (`wait`) passes a turn without moving.

## How a turn works

`CustomForgeEntity` handles the input in `_PhysicsProcess`: on a press it moves the body with `MoveAndCollide`, then calls `EffectsManager.UpdateEffects(1f)`, `Abilities.UpdateAbilities(1f)` and `Abilities.FixedUpdateAbilities(1f)`. That is the whole clock. Nothing else in the demo ticks Forge, so between presses every effect is frozen mid-duration — which is also why the health bar starts at 800 of 1000 and climbs only as you act: `natural_regen.tres` is on the character, but its +1 a second lands once per turn.

## The entity without the node

The character does not use the `ForgeEntity` node. `CustomForgeEntity` is a `CharacterBody2D` that implements `IForgeEntity` itself, which is the recipe for a game whose own base class has to be the entity. In `_Ready` it builds `Tags` from an exported `ForgeTagContainer`, `Attributes` from its `ForgeAttributeSet` children, its own `EffectsManager`, `EntityAbilities`, `EventManager` and shared `Variables`, then runs an `EffectApplier` over its `ForgeEffect` children so the regen lands at ready — everything `ForgeEntity` normally does for you, spelled out once.

## The board

| Area | Effect | What it shows |
| --- | --- | --- |
| Fire Area | `fire.tres` | 10 elemental damage a turn for ten turns, plus `effect.fire` and the fire VFX; gone the turn `effect.wet` arrives. |
| Damage Area | `damage.tres` | 100 physical on entry. |
| Water Area | `water.tres` | `effect.wet` for five turns with the wet VFX, which puts fire out. |
| Regen Area | `heal.tres` + `regen.tres` | 50 Health now, then 10 a turn for ten turns, stacking up to three times with the magnitudes summed; the regen VFX grows with the stack count. |
| EffectRayCast2D | `water.tres` | `OnStay`: wet while the ray touches you, dry again when it does not. |
| EffectShapeCast2D | `regen.tres` | `OnStay`: Rejuvenation while you stand in it, removed when you step out. |

The four areas are `EffectArea2D` nodes in the default `OnEnter` mode — applied once on entry, never removed by leaving — and the casts are `OnStay`. They are the 2D twins of the Real-Time 3D demo's nodes and use the very same effect resources from `shared/effects/`, so a number that surprises you here can be checked against the real-time version there.

## Cues

`FloatingTextCueHandler2D` spawns `2d_floating_text.tscn` with the magnitude of every `cue.floating.text`, red for a loss. Each `ParticlesCueHandler2D` keeps a particle scene under the character between a cue's apply and remove, resizes it on update when told to (the regen one reads the stack count), and fires a one-shot scene on execute. `HealthBar2D` is a progress bar bound to Health, drawn above the character.

## Layout

| Path | Contents |
| --- | --- |
| `scenes/` | `2d_level.tscn`, `2d_character.tscn`, `2d_floating_text.tscn` and the particle scenes under `particles/`. |
| `scripts/` | `CustomForgeEntity`, the two cue handlers, `HealthBar2D` and `FloatText2D`. |
| `sprites/` | The two placeholder textures. |
