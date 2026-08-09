# Quick Start Guide

This guide will help you quickly set up and use the Forge system in your Godot project.

> **Note:** For detailed information about how specific Forge systems work (attributes, effects, tags, etc.), please refer to the [core Forge documentation](https://github.com/gamesmiths-guild/forge/blob/main/docs/README.md).

If you'd like to see sample scenes demonstrating the system in action, install the optional `forge_samples` folder along with the plugin, or clone this repository and open it directly. See [The Sample Project](#the-sample-project) below.

## Installation

### Requirements

- Godot 4.7 or later, .NET version.
- .NET SDK 8.0 or later.
- **C# only.** Forge does not support GDScript-only projects.

### Steps

1. Install **Forge Gameplay System** from the Godot Asset Store, via the **Asset Store** tab in the Godot editor.

   When choosing which files to import, keep `Directory.Build.props` selected. MSBuild imports it automatically, and it is what wires Forge into your build without any edit to your `.csproj`. The `forge_samples` folder is optional.

2. If your project has no C# solution yet, create one via `Project > Tools > C# > Create C# solution`.

3. Back in the Godot editor, build your project by clicking `Build` in the top-right corner of the editor.

4. Enable **Forge Gameplay System** in `Project > Project Settings > Plugins`.

> **Already have a `Directory.Build.props`?** Don't let Forge's copy replace it. Skip that file when importing, and add this line to your own instead:
> ```xml
> <Import Project="addons/forge/Forge.props" />
> ```

For manual installation, download the `.zip` attached to the [latest release](https://github.com/gamesmiths-guild/forge-godot/releases/latest) and extract it into your project root — it contains exactly the same files as the Asset Store package.

### The Sample Project

The `forge_samples` folder contains 2D and 3D scenes demonstrating attributes, effects, abilities, cues, and Statescript in a running game. It is optional — skip it during import if you don't want sample code compiled into your project.

The samples declare their own gameplay tags in `forge_samples/forge_samples_tags.tres`. To run them, open the **Tags** dock and click **Find Sources**, which will offer to add it.

## The ForgeManagers Singleton

When you enable the plugin, it automatically registers a `ForgeBootstrap` autoload that initializes the `ForgeManagers` singleton. This singleton provides access to core system managers:

```csharp
// Access the TagsManager and CuesManager from anywhere in your code
var tagsManager = ForgeManagers.Instance.TagsManager;
var cuesManager = ForgeManagers.Instance.CuesManager;

// Request a tag through the TagsManager
var playerTag = Tag.RequestTag(ForgeManagers.Instance.TagsManager, "character.player");
```

The `ForgeManagers` singleton handles:

- Initializing the tag system with registered tags.
- Managing the cue system for audio/visual feedback.
- Providing global access to these systems through a static Instance property.

### Validation Behavior

By default, validation is **enabled** in the Godot editor and during development builds. For exported **Release builds**, validation is automatically **disabled** unless the "Include Debug Symbols" option is checked during the export process. This ensures that validation checks are not run in production builds unless explicitly requested.

## Creating Your First Forge Entity

### Step 1: Create a Character Scene

1. Create a new scene with a CharacterBody2D or CharacterBody3D as the root.
2. Save the scene (e.g., "Player.tscn").

### Step 2: Add Forge Components

1. Add a ForgeEntity node as a child of your character.
2. Add a ForgeAttributeSet node as a child of the ForgeEntity.

Alternatively, you can implement the IForgeEntity interface directly. See the [CustomForgeEntity.cs](https://github.com/gamesmiths-guild/forge-godot/blob/main/forge_samples/2d/scripts/CustomForgeEntity.cs) in the 2D sample scenes for an example. This approach requires more work but gives you more control over your entity.

### Step 3: Define an Attribute Set

1. Create a new C# script in your project (e.g., "PlayerAttributes.cs").
2. Define your attribute set:

```csharp
using Gamesmiths.Forge.Attributes;

public class PlayerAttributes : AttributeSet
{
    public EntityAttribute Health { get; private set; }
    public EntityAttribute Strength { get; private set; }
    public EntityAttribute Speed { get; private set; }

    public PlayerAttributes()
    {
        // Initialize the attributes with the current, min and max values
        Health = InitializeAttribute(nameof(Health), 100, 0, 100);
        Strength = InitializeAttribute(nameof(Strength), 10, 0, 99);
        Speed = InitializeAttribute(nameof(Speed), 5, 0, 10);
    }
}
```

> **Attribute values are integers**, by design — the simulation stays deterministic. When a stat needs decimals, store it scaled (e.g., `Speed = 475` meaning `4.75`) and declare the scale with `InitializeAttribute(nameof(Speed), 475, 0, 10_000, decimalPlaces: 2)`. The attribute then hands presentation code the converted value through `DisplayValue` / `ToDisplayString(...)`, and the `ForgeAttributeSet` inspector shows you what each raw number reads as. Everything you type in the editor — these values, modifier magnitudes, requirement bounds — stays raw. See [Attribute Values Are Integers](https://github.com/gamesmiths-guild/forge/blob/main/docs/attributes.md#attribute-values-are-integers).
 
### Step 4: Configure the Attribute Set

1. Select the ForgeAttributeSet node in your scene.
2. In the Inspector, set "Attribute Set Class" to "PlayerAttributes".
3. Configure initial values for your attributes in the inspector.

### Step 5: Add a Script to Your Character

```csharp
using Godot;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Nodes;

public partial class Player : CharacterBody2D
{
    private ForgeEntity? _forgeEntity;

    public override void _Ready()
    {
        // Get a reference to our ForgeEntity component
        _forgeEntity = GetNode<ForgeEntity>("ForgeEntity");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_forgeEntity is null) return;

        // Get the speed attribute from our entity
        int speed = _forgeEntity.Attributes["PlayerAttributes.Speed"].CurrentValue;

        // Get movement input
        Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

        // Apply movement using the speed attribute
        Velocity = direction * speed * 100;
        MoveAndSlide();
    }

    public int GetHealthValue()
    {
        // Access health attribute directly from the ForgeEntity
        return _forgeEntity?.Attributes["PlayerAttributes.Health"].CurrentValue ?? 0;
    }
}
```

## Working with Tags

Tags are hierarchical identifiers used throughout the Forge system for classification and targeting.

### Using the Tags Editor

Open the **Tags** dock in the right panel of the Godot editor. To add a tag, type its key in the **Tag Name** field using dot notation — for example `character.player` — pick the destination source beside it if the project has more than one, and press Enter.

To remove a tag, click the **🗑️** on its row. That takes it out of *that source only*; if another source also declares it, the tag still exists.

For everything else — organizing tags across sources, the Merged view, and repairing broken references — see [Gameplay Tags](gameplay-tags.md).

### Configuring Entity Tags

1. Select your ForgeEntity node.
2. In the Inspector, locate the "Base Tags" property.
3. Expand the Container Tags property.
4. In the container, mark the checkbox next to the desired tags.

### Checking Tags in Code

```csharp
// Check if an entity has a tag
bool isPlayer = forgeEntity.Tags.AllTags.HasTag(
    Tag.RequestTag(ForgeManagers.Instance.TagsManager, "character.player"));

// Check for tag inheritance (will match "character.player.wizard" too)
bool isCharacter = forgeEntity.Tags.AllTags.HasTag(
    Tag.RequestTag(ForgeManagers.Instance.TagsManager, "character"));
```

Note: To add or remove tags at runtime, you need to use effects with ModifierTagsEffectComponent. See the [core Forge documentation](https://github.com/gamesmiths-guild/forge/blob/main/docs/quick-start.md) for examples.

## Creating and Applying Effects

The plugin offers an easy way to define new effects through resource files.

### Step 1: Create an Effect Resource

1. Right-click in the FileSystem panel and select "New Resource...".
2. Choose "ForgeEffectData" as the resource type.
3. Save the resource (e.g., "DamageEffect.tres").

### Step 2: Configure the Effect

1. Select the effect resource in the FileSystem panel.
2. In the Inspector:
   - Set "Name" to "Damage".
   - Set "Duration Type" to "Instant" (for immediate damage).
   - Add a modifier:
     - Click "+ Add Element" under "Modifiers" to add one.
     - Create a "New ForgeModifier".
     - Set "Attribute" to "PlayerAttributes.Health".
     - Set "Operation" to "FlatBonus".
     - Set "Calculation Type" to "ScalableFloat".
     - For the "Scalable Float" create a "New ForgeScalableFloat".
     - Set "Base Value" to a negative value (e.g., -10).

### Step 3: Apply the Effect in Code

```csharp
using Godot;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Godot.Nodes;

public partial class Enemy : CharacterBody2D
{
    // Reference the effect resource in the Inspector
    [Export]
    public ForgeEffectData? DamageEffectData { get; set; }

    private void AttackPlayer(Player player)
    {
        if (DamageEffectData == null) return;

        // Get the player's ForgeEntity
        var playerEntity = player.GetNode<ForgeEntity>("ForgeEntity");

        // Create and apply the effect
        var effectData = DamageEffectData.GetEffectData();
        var effect = new Effect(effectData, new EffectOwnership(this, this));
        playerEntity.EffectsManager.ApplyEffect(effect);
    }
}
```

## Using Area-Based Effects

The following principles apply similarly to EffectRayCast2D/3D and EffectShapeCast2D/3D nodes, with appropriate collision configuration for each node type.

### Step 1: Create a Damage Area

1. Add an EffectArea2D (or EffectArea3D) node to your scene.
2. Configure its collision shape.
3. Set "Area Owner" to the node that should be considered the owner of the effects, if any.
4. Set "Trigger Mode" to "OnEnter" to apply effects when entities enter the area.

### Step 2: Add Effect to the Area

1. Add a ForgeEffect node as a child of the EffectArea.
2. In the Inspector, set "Effect Data" to your effect resource.

### Step 3: Ensure Target Entities Have ForgeEntity Components

Any entity that enters the area must have a ForgeEntity component or implement IForgeEntity to receive the effect.

## Creating a Simple Ability

Let’s add a “SimpleAttack” ability to your player. This covers the full process: making the effect, the ability, a simple ability behavior, granting to the player, and using it in gameplay.

### Step 1: Create the Attack Effect Resource

1. Right-click in the FileSystem panel, choose **New Resource > ForgeEffectData**.
2. Save as `simple_attack_effect.tres`.
3. In the Inspector, configure:
   - `Name`: "PlayerAttack"
   - `Duration Type`: "Instant"
   - _Under Modifiers:_
     1. Add a new ForgeModifier.
     2. `Attribute`: `"PlayerAttributes.Health"`
     3. `Operation`: `FlatBonus`
     4. `Calculation Type`: `ScalableFloat`
     5. For "Scalable Float," create a new ForgeScalableFloat, set its **Base Value** to (e.g.) `-20` for damage.

### Step 2: Implement the Ability Behavior

Create a new script called `SimpleAttackAbilityBehavior.cs`:

```csharp
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Godot.Resources;
using Godot;

// This script must have [Tool] and [GlobalClass] so it can be assigned in the inspector
[Tool]
[GlobalClass]
public partial class SimpleAttackAbilityBehavior : ForgeAbilityBehavior
{
    [Export]
    public required ForgeEffectData AttackEffect { get; set; }

    public override IAbilityBehavior GetBehavior()
    {
        // Wrap effect application logic for the ability
        return new SimpleAttackBehavior(AttackEffect.GetEffectData());
    }
}

public sealed class SimpleAttackBehavior : IAbilityBehavior
{
    private readonly EffectData _effectData;
    private Effect? _attackEffect;

    public SimpleAttackBehavior(EffectData effectData)
    {
        _effectData = effectData;
    }

    public void OnStarted(AbilityBehaviorContext context)
    {
        _attackEffect ??= new Effect(
            _effectData,
            new EffectOwnership(context.Owner, context.Source)
        );

        // (Optional) If using cooldowns via cooldown effect, commit here
        context.AbilityHandle.TryCommitCooldown();

        context.Target!.EffectsManager.ApplyEffect(_attackEffect);

        context.InstanceHandle.End();
    }

    public void OnEnded(AbilityBehaviorContext context) { }
}
```

### Step 3: Create the Ability Resource

1. Right-click, **New Resource > ForgeAbilityData**. Save as `simple_attack_ability.tres`.
2. In Inspector:
   - `Name`: "SimpleAttack"
   - Assign your `SimpleAttackAbilityBehavior` script as the `AbilityBehavior`.
   - Under the behavior, set the `AttackEffect` property to your `simple_attack_effect.tres`.
   - (Optional) Add tags, cooldown, or requirements as desired.

### Step 4: Grant the Ability to the Player

In most games, you want the player to always have their basic attack. For clarity, grant this ability permanently and save the handle for activation.

Update your Player script:

```csharp
using Godot;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Godot.Resources.Abilities;

public partial class Player : CharacterBody2D
{
    private ForgeEntity? _forgeEntity;
    private AbilityHandle? _attackHandle;

    [Export]
    public ForgeAbilityData? SimpleAttackAbilityData { get; set; }

    public override void _Ready()
    {
        _forgeEntity = GetNode<ForgeEntity>("ForgeEntity");
        if (_forgeEntity != null && SimpleAttackAbilityData != null)
        {
            // Grant ability permanently and get handle
            _attackHandle = _forgeEntity.Abilities.GrantAbilityPermanently(
                SimpleAttackAbilityData.GetAbilityData(),
                abilityLevel: 1,
                levelOverridePolicy: Core.LevelComparison.None,
                sourceEntity: null // or the object granting the ability
            );
        }
    }

    public override void _Input(InputEvent @event)
    {
        // On attack input (e.g., pressing "attack"), activate the ability
        if (@event.IsActionPressed("attack") && _attackHandle != null)
        {
            // Target selection logic here; for example, get nearest enemy
            // For simplicity, suppose target is set externally
            IForgeEntity? target = GetNearestEnemy();
            if (target != null)
            {
                _attackHandle.TryActivate(out var failures, target);
            }
        }
    }

    private IForgeEntity? GetNearestEnemy()
    {
        // Your targeting logic ...
        return null;
    }
}
```

**Note:**

You can also grant abilities via effects using the **GrantAbility** effect component, which links the ability's lifetime to the effect (for temporary or conditional grants). To use this method, add a `ForgeEffectData` with a **GrantAbility** component, apply the effect to the entity, and fetch the handle with `TryGetAbility()`.

**Summary:**
- Create a `ForgeEffectData` for your damage.
- Create an ability behavior (with `[Tool]` and `[GlobalClass]`).
- Create a `ForgeAbilityData` and assign the behavior/effect to it.
- Add the ability to your player (via API or effect) and hold onto the handle.
- Call `Activate` on input, passing the intended target.

This pattern works for simple and complex abilities alike; just expand the effect logic and ability behavior for more advanced needs.

## Setting Up Visual Feedback with Cues

### Step 1: Create a Cue Handler

```csharp
using Godot;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Godot.Nodes;

[GlobalClass]
public partial class DamageCueHandler : ForgeCueHandler
{
    // Reference a visual effect scene in the Inspector
    [Export]
    public PackedScene? DamageEffectScene { get; set; }

    public override void _CueOnExecute(IForgeEntity forgeEntity, CueParameters? parameters)
    {
        // Ensure we have valid parameters and scene
        if (parameters == null || DamageEffectScene == null) return;
        if (forgeEntity is not Node node) return;

        // Create the visual effect
        Node2D effect = DamageEffectScene.Instantiate<Node2D>();
        GetTree().Root.AddChild(effect);

        // Position the effect at the entity
        if (node.GetParent() is Node2D node2D)
        {
            effect.GlobalPosition = node2D.GlobalPosition;
        }

        // Scale the effect based on damage amount
        float magnitude = parameters.Value.Magnitude;
        effect.Scale = Vector2.One * Mathf.Clamp(Mathf.Abs(magnitude) / 10f, 0.5f, 2.0f);
    }
}
```

### Step 2: Add the Cue Handler to Your Scene

1. Add your custom cue handler to the scene.
2. Go to the Forge tab and add a new tag for your cue (e.g., "cue.effect.damage").
3. Set the "Cue Tag" property to the cue handler node you created.

### Step 3: Update Your Effect Resource to Trigger the Cue

1. Select your effect resource.
2. In the Inspector, add a cue:
   - Click "+ Add Element" under "Cues".
   - Create a "New ForgeCue".
   - Under "Cue Keys" create a "New ForgeTagContainer"
   - Set the container to match your handler's tag (e.g., "cue.effect.damage").
   - Set "Magnitude Type" to "AttributeValueChange".
   - Select "PlayerAttributes.Health" as your "Magnitude Attribute".

## Next Steps

- Learn more about the [Forge nodes](nodes.md) provided by the plugin.
- Browse all available [Forge resources](resources.md) for configuring effects, abilities, tags, and more.
- Build ability behaviors visually with [Statescript](statescript/README.md).
- Discover [helper classes](helper-classes.md) for streamlining common Forge workflows in Godot.
- Explore the scenes in the `forge_samples` folder.
- Check out the [core Forge documentation](https://github.com/gamesmiths-guild/forge/blob/main/docs/README.md) for advanced topics and reference.
