# Demo 01 — Real-Time 3D

A player, two enemies that walk at it and swing, four abilities on an action bar, and a floor of effect areas that interact with each other. Every ability behavior here is C#: an `IAbilityBehavior` implementation under `scripts/abilities/`, wrapped in a `ForgeAbilityBehavior` resource so the ability `.tres` can point at it. `03_statescript` rebuilds the same set as graphs on a bigger level; the two demos are meant to be read side by side.

Movement is WASD or the arrow keys. Aim is the mouse: the abilities that need a direction fire toward where the cursor meets the floor. The camera is bolted to the player.

## Who is in it

Everyone runs the shared `CharacterAttributes` (Health and Mana at 1000) and `MetaAttributes` (the `IncomingDamage` that `DamageExecution` turns into a Health loss) sets from `shared/scripts/attribute_sets/`, and carries `natural_regen.tres`: +1 Health and +1 Mana a second, forever. Player and enemies alike are tagged `trait.damageable`, `trait.flammable`, `trait.healable` and `trait.wettable`, which is what every area below checks for. Each is a `ForgeEntity` node under its body, with the attribute sets, the regen and a grant-abilities effect as children.

## The abilities

| Ability | Key | Behavior | What it does |
| --- | --- | --- | --- |
| Projectile | Mouse 1 or `1` | `ProjectileAbilityBehaviorImplementation` | Commits 5 mana and a 0.2 s cooldown, spawns `scenes/projectile.tscn` a unit along the aim and launches it at 10 units per second. The projectile is a Forge entity of its own, tagged `trait.flammable`; on hitting a body it applies `projectile_damage` — 100 physical, scaled by the distance it flew, from full at point-blank down to a tenth by nine units — with the player as owner and itself as source. |
| Dash | Space or `2` | `DashAbilityBehaviorImplementation` | Commits 20 mana and a 3 s cooldown, writes a velocity of 20 units per second along the aim, clears the first collision-mask bit so the body passes through whatever sits on that layer, and ends itself after 0.25 s through an `AbilityDelayTimer`; `OnEnded` restores the mask. `movement.block` and `immunity.damage` are the ability's activation-owned tags, so the controller stops steering and the damage effects that check the tag — every one of them but fire — do not land. |
| Shield | `3` | `ShieldAbilityBehaviorImplementation` | A toggle: `3` raises it, `3` again cancels it. Raising commits 10 mana and applies `shield_protection` (80% less `IncomingDamage`, infinite) with the `cue.vfx.shield` cue; every `event.damage.taken` commits the cost again, and a Mana listener ends the ability under 10. `OnEnded` removes the effect, the cue and both subscriptions. |
| Thorns | passive | `ReflectAbilityBehaviorImplementation` | Triggered by `event.damage.taken`, the event `DamageExecution` raises, and only for a `DamageType.Physical` payload. Commits 20 mana and a 3 s cooldown, then applies `reflect_damage` set to twice the damage taken to every entity in `%ReflectArea`, fading from full damage within a unit to a tenth just past four, and fires `cue.vfx.reflect`. The reflected damage is `Magical`, so it can neither reflect again nor spread fire. |
| Enemy Attack | — | `EnemyAttackBehaviorImplementation` | The enemy's only ability. Commits the 1.5 s cooldown — which also grants `movement.block`, so the enemy stands still after each swing — and applies `enemy_damage` (100 physical) to the target. |

One more ability is never on the bar: `abilities/common/spread_fire.tres`. `fire.tres` grants it to whatever is burning, and it triggers on `event.damage.dealt` with a physical payload and applies fire to the target. That is how a burning enemy's swing, or a projectile that flew through the Fire Area, sets its victim alight.

## The enemies

`SimpleEnemy3D` walks straight at `%Player` at 2 units per second unless `movement.block` is on, and every frame the player is inside its attack-range area it tries to activate the attack — the cooldown decides whether that succeeds. A Health listener frees the node at zero. Two of them are placed in the level.

## The areas

| Area | Effect | What it shows |
| --- | --- | --- |
| Fire Area | `fire.tres` | Enter and burn: 10 elemental damage a second for 10 s, `effect.fire`, the fire VFX cue and the Spread Fire ability for as long as it lasts. Needs `trait.flammable`, and is removed the moment `effect.wet` shows up. |
| Damage Area | `damage.tres` | 100 physical on entry, unless `immunity.damage` is up. |
| Water Area | `water.tres` | `effect.wet` for 5 s with the wet VFX, which puts fire out and keeps it out. Needs `trait.wettable`. |
| Regen Area | `heal.tres` + `regen.tres` | An instant 50 Health, then Rejuvenation: 10 Health a second for 10 s, stacking up to three times with the magnitudes summed; the regen VFX cue reads the stack count. Needs `trait.healable`. |
| RayCast3D | `water.tres` | An `EffectRayCast3D` in `OnStay` mode: wet while the ray touches you, dry again when it does not. |
| ShapeCast3D | `regen.tres` | An `EffectShapeCast3D` in `OnStay` mode: a sphere that keeps Rejuvenation applied while you stand in it and removes it when you leave. |

The four areas are `EffectArea3D` nodes in the default `OnEnter` mode, so the effect is applied once when a body enters and stepping out changes nothing — the fire keeps burning until the lake, or the timer, puts it out. The casts are `OnStay`: added on enter, removed on exit.

## The HUD and the cues

`Character3D` mirrors ability state into four `ActionBarView`s (`shared/ActionBarView.tscn`): key, cooldown wedge and remaining time, cost. A `TagsView` label lists every tag on the player, which is the quickest way to watch `effect.fire`, `cooldown.skill.dash` or `immunity.damage` come and go. Health and Mana bars are `HealthBar3D` progress bars drawn in a `SubViewport` above each body.

Cues are answered by `ForgeCueHandler` nodes in the level, one per cue tag. `FloatingTextCueHandler3D` spawns `3d_floating_text.tscn` with the cue's magnitude on `cue.floating.text` — every damage and heal effect carries that cue with `AttributeValueChange` on Health as the magnitude, red when it is a loss. Each `ParticlesCueHandler3D` keeps a particle scene alive under the entity between a cue's apply and remove, resizes it on update when told to (the regen one does), and fires a one-shot scene on execute.

## Layout

| Path | Contents |
| --- | --- |
| `abilities/` | One folder per player ability with its `ForgeAbilityData`, cost, cooldown and damage effects; the enemy's attack, cooldown and damage; `common/spread_fire.tres`; and the two grant effects. |
| `scenes/` | `abilities_level.tscn`, `3d_character.tscn`, `simple_3d_enemy.tscn`, `projectile.tscn`, `3d_floating_text.tscn` and the particle scenes under `particles/`. |
| `scripts/` | The player, the enemy, the views and cue handlers, the floor shader, and the ability behaviors under `abilities/`. |

## The C# here

| Script | Job |
| --- | --- |
| `Character3D` | Movement at 5 units per second, the mouse-to-floor ray that gives directional abilities their `TargetData`, the toggle strategy for the shield, and the HUD mirror. It skips input while `movement.block` is on, which is what lets the dash's velocity write last. |
| `SimpleEnemy3D` | Chase, attack-range polling, and `QueueFree` at zero Health. |
| `*AbilityBehavior` / `*AbilityBehaviorImplementation` | Pairs: the `ForgeAbilityBehavior` resource an ability `.tres` points at, and the `IAbilityBehavior` it creates. `AbilityDelayTimer` is the dash's timer node. |
| `Projectile` | Straight-line flight, the distance falloff, and the `EffectApplier` call that lands the damage on whatever it hits. |
| `ActionBarView`, `HealthBar3D`, `FloatText3D` | The views. `EntityView3D`, which lists every attribute of an entity as labels, is not in the level; it is the smallest example of reading attributes from UI. |
| `FloatingTextCueHandler3D`, `ParticlesCueHandler3D` | The cue handlers, reused as they are by the Statescript demo. |
