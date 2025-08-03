# Forge Nodes

This page documents all the custom nodes provided by the Forge for Godot plugin.

## Core Nodes

### ForgeEntity

The central node for adding Forge Framework functionality to any game object.

**Properties:**

- `BaseTags` (ForgeTagContainer): Container for the entity's base tags.

**Description:**

ForgeEntity implements the `IForgeEntity` interface and provides a ready-to-use entity that can be added as a child to any node. It automatically initializes Forge components and manages effects.

**Usage:**

```csharp
// Getting a reference to the ForgeEntity node
var forgeEntity = GetNode<ForgeEntity>("ForgeEntity");

// Accessing attributes
int health = forgeEntity.Attributes["PlayerAttributes.Health"].CurrentValue;

// Checking tags
bool hasFireTag = forgeEntity.Tags.CombinedTags.HasTag(Tag.RequestTag(ForgeManagers.Instance.TagsManager, "element.fire"));
```

### ForgeAttributeSet

A configuration node for attribute sets used with ForgeEntity.

**Properties:**

- `AttributeSetClass` (string): The name of the C# class that extends AttributeSet.
- `InitialAttributeValues` (Dictionary): Configuration values for attributes.

**Description:**

ForgeAttributeSet allows you to configure attribute sets directly in the Godot editor. It uses reflection to instantiate and configure the specified attribute set class.

**Usage:**

```csharp
// Define your attribute set class
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

// Then reference this class in the ForgeAttributeSet node's AttributeSetClass property
// and configure the initial values in the Inspector
```

## Effect Nodes

### ForgeEffect

A node that references an effect data resource.

**Properties:**

- `EffectData` (ForgeEffectData): The effect data resource.

**Description:**

ForgeEffect is used to reference effect data within the scene tree. When added as a child of certain nodes (like ForgeEntity or effect area nodes), it may be automatically applied.

**Usage:**

```csharp
// Get effect data from a ForgeEffect node
var effectNode = GetNode<ForgeEffect>("DamageEffect");
var effectData = effectNode.EffectData.GetEffectData();
```

### EffectArea2D / EffectArea3D

Applies effects to entities that enter, stay in, or exit an area.

**Properties:**

- `AreaOwner` (Node): The entity that owns/causes this effect.
- `TriggerMode` (EffectTriggerMode): When to apply effects (OnEnter, OnExit, OnStay).

**Description:**

These nodes extend Godot's Area2D/Area3D nodes to apply effects to entities. Child ForgeEffect nodes define which effects are applied.

The `OnStay` trigger does not constantly re-apply effects every frame. Instead, it applies effects when an entity enters the area and removes them when the entity exits. This behavior makes it ideal for effects with infinite duration, which will remain active exactly as long as the entity stays in the area. With instant effects, `OnStay` behaves similarly to `OnEnter`. With fixed-duration effects, if the effect expires before the entity leaves the area, nothing happens; if the entity exits before the effect expires, the effect is removed prematurely.

**Usage:**

1. Add an EffectArea2D node
2. Add a CollisionShape2D as child
3. Set AreaOwner to your player/enemy node
4. Set TriggerMode (OnEnter/OnExit/OnStay)
5. Add ForgeEffect nodes as children

### EffectRayCast2D / EffectRayCast3D

Applies effects to entities hit by a raycast.

**Properties:**

- `AreaOwner` (Node): The entity that owns/causes this effect.
- `TriggerMode` (EffectTriggerMode): When to apply effects (OnEnter, OnExit, OnStay).

**Description:**

These nodes extend Godot's RayCast2D/RayCast3D nodes to apply effects to hit entities. Child ForgeEffect nodes define which effects are applied.

The `OnStay` trigger behavior works the same as described for EffectArea nodes.

**Usage:**

- Configure in the editor similar to EffectArea2D/3D.
- The effects will be applied to entities hit by the raycast.

### EffectShapeCast2D / EffectShapeCast3D

Applies effects to entities hit by a shape cast.

**Properties:**

- `AreaOwner` (Node): The entity that owns/causes this effect.
- `TriggerMode` (EffectTriggerMode): When to apply effects (OnEnter, OnExit, OnStay).

**Description:**

These nodes extend Godot's ShapeCast2D/ShapeCast3D nodes to apply effects to hit entities. Child ForgeEffect nodes define which effects are applied.

The `OnStay` trigger behavior works the same as described for EffectArea nodes.

**Usage:**

- Configure in the editor similar to EffectArea2D/3D.
- The effects will be applied to entities hit by the shape cast.

## Cue Nodes

### ForgeCueHandler (Abstract)

Base class for implementing handlers for visual and audio feedback.

**Properties:**

- `CueTag` (string): The tag that identifies which cue events this handler responds to.

**Description:**

ForgeCueHandler is an abstract class that you extend to create custom handlers for audio/visual feedback. It automatically registers itself with the CuesManager using the specified tag.

**Usage:**

```csharp
// Create a custom cue handler
public partial class DamageCueHandler : ForgeCueHandler
{
    [Export]
    public PackedScene? ParticleEffect { get; set; }

    // Called when an effect with this cue is applied
    public override void _CueOnApply(IForgeEntity forgeEntity, CueParameters? parameters)
    {
        // Spawn initial effect
    }

    // Called when an effect with this cue executes (instant and periodic effects only)
    public override void _CueOnExecute(IForgeEntity forgeEntity, CueParameters? parameters)
    {
        // Spawn particles, play sounds, etc.
        if (parameters == null || ParticleEffect == null) return;
        if (forgeEntity is not Node node) return;

        // Create and position the effect
        var effect = ParticleEffect.Instantiate();
        GetTree().Root.AddChild(effect);
        // Position and configure based on parameters...
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

## Helper Classes

### ForgeBootstrap

An autoload singleton that initializes the Forge Framework.

**Description:**

ForgeBootstrap is automatically registered as an autoload when the plugin is enabled. It initializes the core managers (TagsManager, CuesManager) and makes them available through the ForgeManagers.Instance static property.

**Usage:**

```csharp
// Access managers from anywhere
var tagsManager = ForgeManagers.Instance.TagsManager;
var cuesManager = ForgeManagers.Instance.CuesManager;

// Register a tag
var tag = Tag.RequestTag(tagsManager, "ability.damage.fire");
```

### EffectApplier

A helper class for applying effects from nodes.

**Description:**

The EffectApplier simplifies the process of applying effects from child ForgeEffect nodes to target entities.

**Usage:**

```csharp
// Create an applier
var effectApplier = new EffectApplier(this); // Will use ForgeEffect children

// Apply effects once
effectApplier.ApplyEffects(targetNode, this);

// Add effects that persist until removed
effectApplier.AddEffects(targetNode, this);

// Remove previously added effects
effectApplier.RemoveEffects(targetNode);
```
