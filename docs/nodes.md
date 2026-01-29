# Forge Nodes

This page documents all custom nodes provided by the Forge for Godot plugin.

## Core Nodes

### ForgeEntity

The central node for adding Forge system functionality to any game object.

**Properties:**

- `BaseTags` (ForgeTagContainer): Container for the entity's immutable tags.

**Description:**

`ForgeEntity` implements the `IForgeEntity` interface and provides a ready-to-use component for any Godot node. It automatically initializes Forge attributes, tags, effects, and abilities.

**Usage:**

```csharp
// Get a reference to ForgeEntity
var forgeEntity = GetNode<ForgeEntity>("ForgeEntity");

// Access attributes
int health = forgeEntity.Attributes["PlayerAttributes.Health"].CurrentValue;

// Check tags
bool hasFireTag = forgeEntity.Tags.CombinedTags.HasTag(
    Tag.RequestTag(ForgeManagers.Instance.TagsManager, "element.fire"));
```

### ForgeAttributeSet

Configuration node for attribute sets used with ForgeEntity.

**Properties:**

- `AttributeSetClass` (string): Name of the C# class extending AttributeSet.
- `InitialAttributeValues` (Dictionary): Start values for attributes.

**Description:**

`ForgeAttributeSet` lets you configure attribute sets directly in the Godot editor. It uses reflection to instantiate and apply initial values for any custom AttributeSet.

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

## Cue Nodes

### ForgeCueHandler (Abstract)

Base node for implementing handlers for visual and audio feedback.

**Properties:**

- `CueTag` (string): The gameplay cue tag this handler responds to.

**Description:**

Extend `ForgeCueHandler` to implement custom logic for visual/audio response to gameplay events. Registers and unregisters with the Forge CuesManager automatically.

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

## Best Practices

- Use **ForgeEntity** for any game object needing Forge's systems.
- Use **EffectArea/RayCast/ShapeCast** for persistent environment effects and hazards, prefer these over custom code for triggers, traps, or fields.
- Use **ForgeEffect** as a child to define the effects any effect-applier node will use.
- Implement custom **ForgeCueHandler** nodes for all presentation feedback tied to gameplay events.
- When in doubt, favor the provided nodes and resources, they handle complex cases (ownership, cleanup, stacking) automatically.
