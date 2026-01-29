# Forge Resources

This page documents the custom Resource types used by the Forge for Godot plugin. These resources let you define gameplay data (attributes, effects, tags, queries, abilities, and more) directly in the Godot editor.

## Tag & Query Resources

### ForgeTag

Defines a tag string reusable throughout Forge.

**Properties:**

- `Tag` (string): The tag string (e.g., `"element.fire"`).

**Usage:**

Assign as part of tag containers or resource properties.

### ForgeTagContainer

A collection of tags used for requirements, targeting, filtering, etc.

**Properties:**

- `ContainerTags` (Array\<string\>): List of tag strings.

**Usage:**

Assign to ForgeEntity, abilities, requirements, effects, etc.

### QueryExpression

**Advanced**: Compose tag logic (AND, OR, NOT, nested) for effect/ability requirements.

**Properties:**

- `ExpressionType` (TagQueryExpressionType): Main operation for this expression.
- `Expressions` (Array\<QueryExpression\>?): Sub-expressions (for AND/OR/NOT).
- `TagContainer` (ForgeTagContainer?): Tags for single-expression operations.

**Usage:**

Use in tag requirements when basic include/exclude isn’t enough.

## Effect Resources

### ForgeEffectData

Describes all aspects of an effect: what it does and how it behaves.

**Properties:**

- `Name` (string): Display name.
- `Modifiers` (Array\<ForgeModifier\>): Attribute changes.
- `Components` (Array\<ForgeEffectComponent\>): Modular behaviors (requirements, tag changes, etc.).
- `DurationType` (DurationType): Instant/Timed/Infinite.
- `Duration` (ForgeModifierMagnitude?): How long the effect lasts (if timed).
- `CanStack`, `StackPolicy`, etc.: How multiple applications interact.
- `Cues` (Array\<ForgeCue\>): Audio/visual feedback links.
- `SnapshotLevel` (bool): If the effect's level is fixed at application time.

**Usage:**

Assign to ForgeEffect nodes, ability costs/cooldowns, or code-driven effects.

### ForgeModifier

Configures a single attribute change applied by an effect.

**Properties:**

- `Attribute` (string): Attribute to modify (full path, e.g. `"PlayerAttributes.Health"`).
- `Operation` (ModifierOperation): Add, multiply, or override.
- `CalculationType` (MagnitudeCalculationType): Magnitude method.
- Calculation parameters: (`ScalableFloat`, `CapturedAttribute`, etc), depending on type.

**Usage:**

In the `Modifiers` array of a ForgeEffectData.

### ForgeModifierMagnitude

Controls how a modifier's value is calculated (fixed, attribute-based, set-by-caller, or custom algorithm).

**Properties:**

- `CalculationType`
- Type-specific fields.

### ForgeScalableFloat, ForgeScalableInt

Level or context-dependent values, optionally shaped by a Godot `Curve`.

**Properties:**

- `BaseValue` (float/int)
- `ScalingCurve` (Curve)

**Usage:**

For values that should scale (damage, duration, stack limits, etc).

### ForgeEffectComponent (Abstract)

Base for modular effect logic (requirements, tag changes, application chance, etc).

To create a new component:

- Inherit from it.
- Use `[Tool]` and `[GlobalClass]` so it appears in the Godot Inspector.
- **Override `GetComponent()`.**

**Usage Example:**

```csharp
[Tool]
[GlobalClass]
public partial class MyBuffComponent : ForgeEffectComponent
{
    public override IEffectComponent GetComponent()
    {
        // Custom effect logic...
    }
}
```

Attach your script as a resource to the `Components` array of any ForgeEffectData.

### Built-in Effect Components

- **ChanceToApplyEffect**: Adds a chance for an effect to be applied.
- **GrantAbility**: Grants one or more abilities when active.
- **ModifierTags**: Adds tags to the target when the effect is applied.
- **TargetTagRequirements**: Sets tag/query-based application/ongoing/removal requirements.

All of these extend `ForgeEffectComponent` and can be added to effect data in the inspector.

## Cue Resources

### ForgeCue

Describes a gameplay cue for VFX, SFX, UI, etc.

**Properties:**

- `CueKeys` (ForgeTagContainer): Tag(s) for filtering.
- `MinValue`, `MaxValue` (int): Magnitude range.
- `MagnitudeType` (CueMagnitudeType): How magnitude is calculated.
- `MagnitudeAttribute` (string): Attribute used for magnitude, if applicable.

**Usage:**

Assign in a ForgeEffectData `Cues` list; matched at runtime by a ForgeCueHandler.

## Ability Resources

### ForgeAbilityData

Describes, configures, and links a gameplay ability.

**Properties:**

- `Name` (string): Ability identifier.
- `InstancingPolicy` (AbilityInstancingPolicy): Controls concurrent runs.
- `CooldownEffects` (ForgeEffectData[]): Effects for cooldown logic.
- `CostEffect` (ForgeEffectData): Effect applied as a cost.
- `AbilityBehavior` (ForgeAbilityBehavior): Custom ability logic (see below).
- Tag filters: `AbilityTags`, `ActivationRequiredTags`, etc.

**Usage:**

Add to GrantAbility component, trigger via scripts/abilities.

### ForgeAbilityBehavior (Abstract)

Implements the logic for an ability.

- **To extend:** Inherit, use `[Tool]` and `[GlobalClass]`, and override `GetBehavior()`.

**Usage Example:**

```csharp
[Tool]
[GlobalClass]
public partial class MyDashBehavior : ForgeAbilityBehavior
{
    public override IAbilityBehavior GetBehavior()
    {
        // Return your ability logic
    }
}
```

Assign in a ForgeAbilityData resource for custom behavior.

## Advanced / Extensible API Resources

### ForgeCustomCalculator (Abstract)

For advanced custom magnitude calculation logic.

**To extend:** Inherit, use `[Tool]` and `[GlobalClass]`, override `GetCustomCalculatorClass()`.

**Usage Example:**

```csharp
[Tool]
[GlobalClass]
public partial class MyCriticalChanceCalculator : ForgeCustomCalculator
{
    public override CustomModifierMagnitudeCalculator GetCustomCalculatorClass()
    {
        // Your custom calculation logic
    }
}
```
Reference as the "Custom Calculator Class" in a ForgeModifier.

### ForgeCustomExecution (Abstract)

Advanced logic for effects modifying multiple attributes or orchestrating custom logic.

**To extend:** Inherit, use `[Tool]` and `[GlobalClass]`, override `GetExecutionClass()`.

**Usage Example:**

```csharp
[Tool]
[GlobalClass]
public partial class MyStunExecution : ForgeCustomExecution
{
    public override CustomExecution GetExecutionClass()
    {
        // Your custom execution logic
    }
}
```

Reference in the `Executions` array in ForgeEffectData.

## General Notes

- When making custom resource scripts, always use `[Tool]` and `[GlobalClass]` so they're visible in the Inspector.
- All built-in resources are in the `Gamesmiths.Forge.Godot.Resources` namespace.
