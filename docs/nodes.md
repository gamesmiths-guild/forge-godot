# Forge Nodes

This page documents all custom nodes provided by the Forge for Godot plugin.

## Core Nodes

### ForgeEntity

The central node for adding Forge system functionality to any game object.

**Properties:**

- `BaseTags` (ForgeTagContainer): Container for the entity's immutable tags.
- `SharedVariableDefinitions` (ForgeSharedVariableSet): Defines shared variables accessible by all Statescript graphs running on this entity.

**Description:**

`ForgeEntity` implements the `IForgeEntity` interface and provides a ready-to-use component for any Godot node. It automatically initializes Forge attributes, tags, effects, abilities, and shared variables.

Additionally, `ForgeEntity` automatically calls `EffectsManager.UpdateEffects` and `Abilities.UpdateAbilities` each frame in its `_Process` method, driving time-dependent behaviors such as timed effects, periodic effects, and Statescript graph updates. It also calls `Abilities.FixedUpdateAbilities` on each physics step in its `_PhysicsProcess` method, driving the state nodes that move bodies or query physics.

**Usage:**

```csharp
// Get a reference to ForgeEntity
var forgeEntity = GetNode<ForgeEntity>("ForgeEntity");

// Access attributes
int health = forgeEntity.Attributes["PlayerAttributes.Health"].CurrentValue;

// Check tags
bool hasFireTag = forgeEntity.Tags.AllTags.HasTag(
    Tag.RequestTag(ForgeManagers.Instance.TagsManager, "element.fire"));
```

#### Observing an Entity from a Godot Node

Forge exposes its runtime state through plain C# `event` members rather than Godot signals, so a UI node subscribes to them directly on the manager that owns the state. These are change notifications — distinct from the Forge [Events system](https://github.com/gamesmiths-guild/forge/blob/main/docs/events.md), which is a tag-routed bus for simulation logic. Four feeds cover everything a HUD reads:

| What you are drawing | Feed | Reached through |
|---|---|---|
| A single value — health bar, mana globe | [`EntityAttribute.OnValueChanged`](https://github.com/gamesmiths-guild/forge/blob/main/docs/attributes.md#from-outside-the-attributeset) | `Entity.Attributes["Set.Attribute"]` |
| Status icons, state-driven visuals | [`EntityTags.OnTagsChanged`](https://github.com/gamesmiths-guild/forge/blob/main/docs/tags.md#reacting-to-tag-changes) | `Entity.Tags` |
| A buff bar | [`EffectsManager` change notifications](https://github.com/gamesmiths-guild/forge/blob/main/docs/effects/README.md#observing-effects) | `Entity.EffectsManager` |
| An ability bar or action slots | [`EntityAbilities` change notifications](https://github.com/gamesmiths-guild/forge/blob/main/docs/abilities.md#observing-abilities) | `Entity.Abilities` |

Each is a plain `event`, so the pattern is the same for all four — subscribe once the entity is ready, unsubscribe when the observer leaves the tree:

```csharp
public partial class EntityHud : Control
{
    [Export]
    public ForgeEntity? Entity { get; set; }

    private EntityAttribute? _health;

    public override void _Ready()
    {
        if (Entity is null)
        {
            return;
        }

        _health = Entity.Attributes["CharacterAttributes.Health"];
        _health.OnValueChanged += HandleHealthChanged;

        Entity.Tags.OnTagsChanged += HandleTagsChanged;

        Entity.EffectsManager.OnActiveEffectAdded += HandleEffectAdded;
        Entity.EffectsManager.OnActiveEffectChanged += HandleEffectChanged;
        Entity.EffectsManager.OnActiveEffectRemoved += HandleEffectRemoved;

        Entity.Abilities.OnAbilityGranted += HandleAbilityGranted;
        Entity.Abilities.OnAbilityChanged += HandleAbilityChanged;
        Entity.Abilities.OnAbilityRemoved += HandleAbilityRemoved;
    }

    public override void _ExitTree()
    {
        if (Entity is null)
        {
            return;
        }

        if (_health is not null)
        {
            _health.OnValueChanged -= HandleHealthChanged;
        }

        Entity.Tags.OnTagsChanged -= HandleTagsChanged;

        Entity.EffectsManager.OnActiveEffectAdded -= HandleEffectAdded;
        Entity.EffectsManager.OnActiveEffectChanged -= HandleEffectChanged;
        Entity.EffectsManager.OnActiveEffectRemoved -= HandleEffectRemoved;

        Entity.Abilities.OnAbilityGranted -= HandleAbilityGranted;
        Entity.Abilities.OnAbilityChanged -= HandleAbilityChanged;
        Entity.Abilities.OnAbilityRemoved -= HandleAbilityRemoved;
    }

    private void HandleHealthChanged(EntityAttribute attribute, int change) =>
        SetHealthBar(attribute.CurrentValue, attribute.Max);

    private void HandleTagsChanged(TagContainer allTags) => RefreshStatusIcons(allTags);

    private void HandleEffectAdded(ActiveEffectHandle handle) => AddBuffIcon(handle);

    private void HandleEffectChanged(ActiveEffectHandle handle) => RefreshBuffIcon(handle);

    private void HandleEffectRemoved(ActiveEffectHandle handle, EffectRemovalReason reason) => RemoveBuffIcon(handle);

    private void HandleAbilityGranted(AbilityHandle handle) => AddAbilitySlot(handle);

    private void HandleAbilityChanged(AbilityHandle handle) => SetSlotEnabled(handle, !handle.IsInhibited);

    private void HandleAbilityRemoved(AbilityHandle handle) => RemoveAbilitySlot(handle);
}
```

Two Godot-specific points:

- **Always unsubscribe in `_ExitTree`.** A `ForgeEntity` you observe from elsewhere — an enemy's health bar, a targeting frame — usually outlives the observer, and a handler on a freed node throws.
- **`ForgeEntity` builds its managers in `_Ready`**, so a node that subscribes must run after it. Rely on Godot's bottom-up `_Ready` order (children first) by subscribing from `_Ready` on an ancestor of the entity, or otherwise defer subscription until after the entity is ready.

Both `ActiveEffectHandle` and `AbilityHandle` are stable for the whole life of what they point at, which makes them good dictionary keys for the icon or slot they belong to. Each is still readable inside its removal handler and invalid immediately after, so read the name, stacks, level or duration there rather than storing the handle for later.

The feeds carry more than the lifecycle shown above. `EffectsManager` also reports applications, executions and denials; `EntityAbilities` reports activation, ending and refused activations — `OnAbilityActivationFailed` is the one to hook for "why did nothing happen when I pressed the button", since it also fires for activations driven by triggers and Statescript nodes. See the linked core pages for the full tables.

### ForgeAttributeSet

Configuration node for attribute sets used with ForgeEntity.

**Properties:**

- `AttributeSetClass` (string): Name of the C# class extending AttributeSet.
- `InitialAttributeValues` (Dictionary): Per-attribute `Default`, `Min` and `Max` overrides for the values the attribute set's constructor declares.

**Description:**

`ForgeAttributeSet` lets you configure attribute sets directly in the Godot editor. It uses reflection to instantiate and apply initial values for any custom AttributeSet.

The class named by `AttributeSetClass` can be one you wrote or one generated from an [attribute set definition](attribute-set-definitions.md) — this node cannot tell the two apart, and neither can anything else that consumes attributes.

> **Attribute values are integers.** `Default`, `Min` and `Max` are all `int`, as are `CurrentValue`, `BaseValue`, `Modifier` and `Overflow` at runtime — a deliberate choice for deterministic simulation. Store fractional stats scaled (a `Speed` of `475` meaning `4.75`) and declare the scale with `InitializeAttribute(..., decimalPlaces: 2)`. See [Attribute Values Are Integers](https://github.com/gamesmiths-guild/forge/blob/main/docs/attributes.md#attribute-values-are-integers) in the core docs.

**Everything in the inspector is authored raw**, including these fields — a `Speed` storing hundredths is typed as `475`, not `4.75`. That is deliberate: a modifier's `ScalableFloat`, an attribute requirement's bounds and a cue's magnitude bounds are all raw too, and several of them (a chance, a period, a coefficient) have no attribute to be scaled by in the first place. One unit everywhere beats a rule that holds in one panel and not the next.

What the inspector does do is **tell you what the raw number reads as**. When the attribute set declares decimal places, the header gains a `— 2 decimals` suffix and each field gains a dimmed reading before it:

```text
Speed — 2 decimals
  Default  (4.74) [  475 ]
  Min      (0.00) [    0 ]
  Max     (10.00) [ 1000 ]
```

**Usage:**

```csharp
// Define your attribute set
public class CharacterAttributes : AttributeSet
{
    public EntityAttribute Health { get; private set; }
    public EntityAttribute Mana { get; private set; }

    public CharacterAttributes()
    {
        Health = InitializeAttribute(nameof(Health), 100, 0, 100);
        Mana = InitializeAttribute(nameof(Mana), 50, 0, 100);
    }
}
// Reference this class in AttributeSetClass property and configure in the Inspector.
```

## Effect Nodes

### ForgeEffect

References a ForgeEffectData resource in the scene.

**Properties:**

- `EffectData` (ForgeEffectData): The effect data resource.

**Description:**

`ForgeEffect` connects an effect definition with node-based effect application in the scene tree. When added as a child of certain nodes, it may be automatically applied to entities or objects.

**Usage:**

```csharp
var effectNode = GetNode<ForgeEffect>("DamageEffect");
var effectData = effectNode.EffectData.GetEffectData();
```

### EffectArea2D / EffectArea3D

Extends Godot's Area2D/Area3D; applies effects to entities that enter, stay in, or exit the area, using child ForgeEffect nodes.

**Properties:**

- `EffectOwner` (Node): Entity ultimately responsible for the effect (e.g., the player who placed the area).
- `EffectSource` (Node): The node causing the effect (e.g., the area itself).
- `EffectLevel` (int): Level of all applied effects.
- `TriggerMode` (EffectTriggerMode): Determines when to apply and remove effects (`OnEnter`, `OnExit`, `OnStay`).

**Description:**

Effect areas are the idiomatic way to implement hazards, traps, fields, and persistent buffs/debuffs.
- **OnEnter:** Applies effects once when an entity enters.
- **OnExit:** Applies effects once when an entity exits.
- **OnStay:** Adds effects on enter, removes on exit.

**Usage:**

1. Add an EffectArea2D/3D node to your scene.
2. Add a CollisionShape as a child.
3. Set EffectOwner and EffectSource in the Inspector or code.
4. Set EffectLevel and TriggerMode as needed.
5. Add ForgeEffect child nodes for each effect.

### EffectRayCast2D / EffectRayCast3D

Extends Godot's RayCast nodes; applies effects to entities hit by the ray, using the same trigger patterns as area nodes.

**Properties:**

- `EffectOwner` (Node): Entity ultimately responsible for the effect (e.g., the player who placed the area).
- `EffectSource` (Node): The node causing the effect (e.g., the area itself).
- `EffectLevel` (int): Level of all applied effects.
- `TriggerMode` (EffectTriggerMode): Determines when to apply and remove effects (`OnEnter`, `OnExit`, `OnStay`).

**Description:**

Ideal for spells, lasers, or line-of-sight triggers. Automatically checks for IForgeEntity on collided objects.

**Usage:**

- Add to scene and set properties.
- Add child ForgeEffect nodes for any effect it should apply on hit.

### EffectShapeCast2D / EffectShapeCast3D

Extends ShapeCast nodes; applies effects to entities detected by a shape cast.

**Properties:**

- `EffectOwner` (Node): Entity ultimately responsible for the effect (e.g., the player who placed the area).
- `EffectSource` (Node): The node causing the effect (e.g., the area itself).
- `EffectLevel` (int): Level of all applied effects.
- `TriggerMode` (EffectTriggerMode): Determines when to apply and remove effects (`OnEnter`, `OnExit`, `OnStay`).

**Description:**

Great for melee sweeps, cone attacks, or custom AoE checks.

**Usage:**

- Add the node, configure shape and properties.
- Add ForgeEffect child nodes as needed.

### ForgeProjectile2D / ForgeProjectile3D

Extends Area2D/Area3D; travels along its own forward each physics step and applies its child ForgeEffect nodes to whatever it hits. The generic projectile, so a fireball, an arrow or a bullet is a scene rather than a C# class.

**Properties:**

- `Speed` (float): Units per second. Defaults to `10` in 3D and `400` in 2D.
- `MaxLifetime` (float): Seconds before it expires. Default `5`.
- `MaxRange` (float): Distance before it expires. Zero means unlimited.
- `Pierce` (int): How many extra targets it hits before `DestroyOnHit` frees it. Zero frees it on the first. With `DestroyOnHit` off it is not a limit at all — the projectile keeps hitting until its lifetime or range ends.
- `DistanceFalloffCurve` (Curve): Sampled at distance travelled over `MaxRange` and passed to the effects as context data.
- `DestroyOnHit` (bool): Default on.
- `IncludeAreas` (bool): Whether areas count as hits, as well as bodies.
- `Swept` (bool): Default on — see below.

**Description:**

Moves −Z in 3D and +X in 2D, so **aim is simply instantiation rotation**: binding [`InstantiateScene3DNode`](statescript/nodes/scene-nodes.md#the-instantiating-pair)'s Rotation to core's `LookAt` — or the 2D node's Rotation to an angle — is the whole launch story, and there is no `Launch` method to call.

It carries `ForgeEffect` children exactly as `EffectArea3D` does, and implements `IInstantiationReceiver`, so owner and source arrive from whatever spawned it. On a hit it resolves the target through [`ForgeEntityBridge`](helper-classes.md#forgeentitybridge) and applies its effects with the falloff sampled from the curve.

**Signals:** `Hit(Node)` and `Expired()`.

**`Swept` closes the tunnelling question, and defaults on.** An area is tested for overlaps once per physics step, so a projectile whose step is longer than a wall is thick was never tested anywhere inside that wall and appeared on the far side of it. Swept, each step is a shape cast along the motion instead, and the projectile is placed *at* the impact rather than past it on the step that ends it.

Two costs come with it. The cast is not free, and **the first enabled `CollisionShape` child becomes the query's shape** — a projectile built from several shapes or from a `CollisionPolygon` has no single shape to sweep and says so with a warning rather than silently sweeping one of them. That is why it is an export and not the only behavior.

Pierce works by **re-sweeping with each collider it met excluded**, so a piercing shot reports everything along the step in the order it met them. The exclusion list is kept for the whole flight rather than per step, so a collider already answered for cannot stop the sweep again. The caster needs no special case: a projectile spawned inside its owner meets the owner on its first cast, skips it as the owner, and the exclusion that follows is exactly what should happen for the rest of the flight.

`Pierce` counts down on every hit, but only `DestroyOnHit` acts on the count reaching zero — so an endlessly piercing beam-like projectile is `DestroyOnHit` off, bounded by `MaxLifetime` or `MaxRange` instead.

While swept, monitoring is switched off — nothing reads the overlap list any more, and an armed area pays for a test per step that decides nothing. The projectile stays *monitorable*, so areas watching for it still see it.

## Cue Nodes

### ForgeCueHandler (Abstract)

Base node for implementing handlers for visual and audio feedback.

**Properties:**

- `CueTag` (string): The gameplay cue tag this handler responds to.

**Description:**

Extend `ForgeCueHandler` to implement custom logic for visual/audio response to gameplay events. Registers and unregisters with the Forge CuesManager automatically. Before writing one, check whether the [cue handler library](#cue-handler-library) below already covers it.

Each phase has **two overloads**: one taking the target entity, and one taking only the parameters. Override the second when the effect belongs to the screen rather than to whoever was hit — that is what the camera shake and hit stop handlers do.

`WarnOnce(message)` reports a misconfiguration — a path pointing at nothing, a curve on an emitter that cannot scale — exactly once per message rather than once per application. One handler serves every target of its cue, so a bad path is wrong for all of them, and suppressing per *message* rather than per handler stops a handler's first problem hiding its second.

**Usage:**

```csharp
[GlobalClass]
public partial class DamageCueHandler : ForgeCueHandler
{
    [Export]
    public PackedScene? ParticleEffect { get; set; }

    // Called when an effect with this cue is applied
    public override void _CueOnApply(IForgeEntity forgeEntity, CueParameters? parameters)
    {
        // E.g., spawn the initial effect
    }

    // Called when an effect with this cue executes (instant and periodic effects only)
    public override void _CueOnExecute(IForgeEntity forgeEntity, CueParameters? parameters)
    {
        // Spawn particles, play sounds, etc.
        if (parameters == null || ParticleEffect == null) return;
        if (forgeEntity is not Node node) return;

        var effect = ParticleEffect.Instantiate();
        GetTree().Root.AddChild(effect);
        // Custom placement based on parameters...
    }

    // Called when an effect with this cue is updated
    public override void _CueOnUpdate(IForgeEntity forgeEntity, CueParameters? parameters)
    {
        // Update ongoing effects
    }

    // Called when an effect with this cue is removed
    public override void _CueOnRemove(IForgeEntity forgeEntity, bool interrupted)
    {
        // Clean up or spawn removal effects
    }
}
```

## Cue Handler Library

Six concrete handlers ship with the plugin, so the common visual and audio hookups need no C# at all. Add one to the scene, set its `CueTag`, and fill in its exports.

All six inherit `CueTag` and `WarnOnce` from [`ForgeCueHandler`](#forgecuehandler-abstract). **(M)** marks an optional `MagnitudeCurve`, sampled at the cue's normalized magnitude.

### ParticlesCueHandler

Drives a particle emitter already in the target's scene, which is what keeps the material, draw pass and local-coords flag where an artist authored them.

- `ParticlesPath` (string): Path to the emitter, from the node the target lives on. Empty means the target's first emitter child.
- `OneShotOnExecute` (bool, default on): Executing restarts the emitter rather than only switching emission on.
- `MagnitudeCurve` (Curve) **(M)**: Written as the emitter's amount ratio.

Applying starts emission, removing stops it, executing bursts. One class covers all four emitter types; they share no base class in Godot, so the operations switch over them. **The magnitude curve reaches only the GPU pair** — `amount_ratio` is the one knob that scales emission without reallocating the particle buffer, and the CPU emitters do not have it, so a curve on one says so rather than silently doing nothing.

### InstantiateSceneCueHandler

The general-purpose visual: a telegraph, an impact burst, a shield bubble, a scorch mark.

- `Scene` (PackedScene)
- `Attach` (CueAttachMode): `TargetEntity` or `World`.
- `Lifetime` (float): Seconds an executed instance lives. Zero or less leaves it to free itself.
- `MagnitudeCurve` (Curve) **(M)**: Multiplied into the scene's authored scale.

Executing spawns and forgets; applying spawns and holds, and removing frees what it spawned — which is what ties a bubble to the effect that put it there. It reads a well-known `position` custom parameter when one is present, else the target's position.

**Placement happens before parenting**, written as a local transform through the parent's, because `AddChild` readies the instance and a scene that measures its own position in `_Ready` would otherwise see the position the scene was authored at.

### AudioCueHandler

- `PlayerPath` (string): An existing audio player. Empty falls back to `Stream`, then to the target's first audio player child.
- `Stream` (AudioStream): Creates a player on the target instead. Ignored when `PlayerPath` resolves.
- `StopOnRemove` (bool, default on): Off lets a tail finish after the effect has gone.
- `MagnitudeCurve` (Curve) **(M)**: Sets the volume as a linear gain where one is full.

Two ways to say what plays: a path, for anything whose bus, attenuation or stream randomization is authored; or a bare stream, for the common case where a cue is one sound and adding a node to every entity that can receive it is the only obstacle. A created player matches the target's dimension, so a sound on a 3D character is positional without the cue saying so.

**One player per target, not one per playback.** A player created per execution and freed on `Finished` never frees for a looping stream, so a hit cue firing ten times a second would stack ten never-freed players a second onto its target. `MaxPolyphony` keeps what made per-playback players attractive — repeated executes still overlap instead of cutting each other off.

**The curve replaces the volume rather than scaling it.** The volume is written *onto* a shared player, so adding to it would compound: each application would read back a level that already included the last one, and the sound would walk up until it clipped. With a curve, the cue decides the level; without one, the player's own mix stands.

### AnimationCueHandler

- `PlayerPath` (string): Empty means the target's first animation player child.
- `ApplyAnimation`, `ExecuteAnimation`, `RemoveAnimation` (string): One clip per phase.

Three names rather than one animation with a mode, because a stun that starts, holds and ends is three different clips and the alternative is three handlers under three cue tags. A phase left empty plays nothing, which makes a one-phase cue a single filled field.

### CameraShakeCueHandler

- `Amplitude` (float, default `0.1`): In the camera's own units — world units in 3D, pixels in 2D.
- `Duration` (float, default `0.25`): For an executed shake. Ignored while a cue is applied, which shakes until removal.
- `MagnitudeCurve` (Curve) **(M)**: Scales the amplitude, so a scratch and a critical do not shake identically.

### HitStopCueHandler

- `TimeScale` (float, default `0.05`)
- `Duration` (float, default `0.08`)
- `MagnitudeCurve` (Curve) **(M)**: Scales the duration, so a heavier hit hangs longer.

Writes `Engine.TimeScale` for a moment and restores whatever was in force before. It is global — that is what a hit stop *is* — which means two of them fighting over the same moment is one handler's stop, so put one in the scene and let cue tags decide when it fires.

### The two screen effects

`CameraShakeCueHandler` and `HitStopCueHandler` are the only handlers that **do not touch their target**: they read the cue's phase and nothing else, which is what "presentation only" turns out to mean in code. They are cue handlers rather than graph nodes for the same reason — nothing about a screen effect belongs to the entity that was hit, and a graph node that could shake the screen would be a node that has to know *which* screen.

Three details they share:

- **The shake writes the camera's offset, never its transform**, because a game's camera is nearly always driven by a rig that would fight a transform write every frame.
- **Both count in wall clock, not frame delta.** A hit stop measured in scaled time at a twentieth speed would last twenty times as long as it says, and a shake driven by scaled time would crawl through exactly the hit stop it exists to punctuate.
- **Both put back what they found** when they end or leave the tree, so a handler freed mid-effect cannot leave the whole game in slow motion with nothing left to undo it.

## Best Practices

- Use **ForgeEntity** for any game object needing Forge's systems.
- Use **EffectArea/RayCast/ShapeCast** for persistent environment effects and hazards, prefer these over custom code for triggers, traps, or fields.
- Use **ForgeProjectile2D/3D** for linear projectiles rather than writing a mover; aim it by spawning it rotated.
- Use **ForgeEffect** as a child to define the effects any effect-applier node will use.
- Reach for the **cue handler library** first, and implement custom **ForgeCueHandler** nodes for presentation it does not cover.
- When in doubt, favor the provided nodes and resources, they handle complex cases (ownership, cleanup, stacking) automatically.
