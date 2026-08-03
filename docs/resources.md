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
- `EffectTags` (ForgeTagContainer?): Identity tags for the effect itself. **Never granted to the target** — they classify the effect so `ForgeEffectQuery` can select it by category. The rule: granted tags (a ModifierTags component) for entity state, effect tags for identity.

**Usage:**

Assign to ForgeEffect nodes, ability costs/cooldowns, or code-driven effects.

### ForgeEffectQuery

Selects effects by identity, by the tags they grant, by their source, or by what they modify. Every field is optional and all filled-in fields are combined with **AND**; a query with nothing set matches every effect.

**Properties:**

- `EffectDefinition` (ForgeEffectData?): The exact effect data the effect was built from.
- `EffectTagQuery` (ForgeQueryExpression?): Matched against the effect's own identity tags.
- `GrantedTagQuery` (ForgeQueryExpression?): Matched against the tags the effect grants to its target.
- `OwningTagQuery` (ForgeQueryExpression?): Matched against both sets together.
- `SourceRequiredTags`, `SourceIgnoredTags` (ForgeTagContainer?), `SourceTagQuery` (ForgeQueryExpression?): Matched against the tags of the entity that applied the effect.
- `ModifyingAttribute` (string): An attribute at least one of the effect's modifiers must target. Picked from the same dropdown tree of scanned attribute sets that `ForgeModifier` uses, which here also offers a **None** entry, since leaving the filter unset is a valid query rather than a missing value.

**Usage:**

Assign to a [QueryActiveEffectsResolver](statescript/resolvers/query-active-effects-resolver.md) to dispel by category, or to an [EffectQueryMatchResolver](statescript/resolvers/effect-query-match-resolver.md) to test a single handle.

The runtime `EffectQuery` also supports matching a specific source entity instance and an arbitrary predicate; neither can be authored as a resource, so both stay code-only.

### ForgeModifier

Configures a single attribute change applied by an effect.

**Properties:**

- `Attribute` (string): Attribute to modify (full path, e.g. `"PlayerAttributes.Health"`).
- `Operation` (ModifierOperation): `FlatBonus` (add), `PercentBonus` (add a percentage), or `Override` (replace).
- `CalculationType` (MagnitudeCalculationType): Magnitude method.
- Calculation parameters: (`ScalableFloat`, `CapturedAttribute`, etc), depending on type.
- `Channel` (int): Which attribute channel the modifier lands in.
- `AggregationMode` (AggregationMode): `Sum` (default), `Max` or `Min`. Greyed out on instant and periodic effects, which execute their modifiers against the base value and have nothing to aggregate.

**Usage:**

In the `Modifiers` array of a ForgeEffectData.

Within a channel, flat bonuses are summed, then percent bonuses are summed and applied. An `Override` replaces the value entering that channel and skips its other modifiers; there is no priority between overrides — the most recently applied one wins, and removing it hands the channel back to the previously applied override. See [Effect Modifiers](https://github.com/gamesmiths-guild/forge/blob/main/docs/effects/modifiers.md) in the core docs for the full evaluation order.

`AggregationMode` changes that summing. Modifiers are grouped by attribute, channel, operation and mode; a `Max` group contributes only its highest value and a `Min` group only its lowest, while the `Sum` group keeps adding up as usual, and the three contributions are then combined. Setting every movement speed buff to `Max` is the declarative way to build "only the strongest buff applies" — when the strongest is removed or expires, the next strongest takes over immediately. Use `Min` for the mirrored case, "only the strongest slow applies", since the comparison is on signed values. For `Override`, the most recently applied override picks the policy: a plain `Sum` override wins outright, while a `Max`/`Min` one hands the channel to the extreme override of that same mode.

When `CalculationType` is `AttributeBased`, `CaptureSource` chooses which entity the attribute is read from: `Owner` (who triggered the effect), `Target` (who receives it), or `Source` (what caused it — useful when a weapon, turret or summon node is itself a `ForgeEntity` with its own attribute set). A capture whose entity is missing, or which lacks the attribute, yields `0`. `SnapshotAttribute` off keeps the magnitude live, re-evaluating whenever the captured attribute changes.

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

- **AdditionalEffects**: Applies conditional effects when this effect is applied or completes, optionally copying its SetByCaller data.
- **AttributeAccumulator**: Accumulates attribute changes and publishes the total as a SetByCaller magnitude.
- **AttributeRequirements**: Sets attribute-value based application/ongoing/removal requirements on the target.
- **BlockAbilityTags**: Blocks abilities carrying the given tags from activating while the effect is active.
- **CancelAbilityTags**: Cancels active abilities selected by tag, on application or on each execution.
- **ChanceToApplyEffect**: Adds a chance for an effect to be applied.
- **GrantAbility**: Grants one or more abilities when active.
- **Immunity**: Blocks incoming effects matching its `ForgeEffectQuery` array while the effect is active. Duration effects only.
- **ModifierTags**: Adds tags to the target when the effect is applied.
- **RaiseEvent**: Raises Forge events at selected effect lifecycle points, optionally with a calculated magnitude.
- **RemoveOther**: Removes active effects matching its `ForgeEffectQuery` array when applied, optionally removing only some stacks.
- **SourceAttributeRequirements**: The same gates as AttributeRequirements, read from the effect's source or owner (controlled by `OwnershipEntity`).
- **SourceTagRequirements**: The same gates as TargetTagRequirements, read from the effect's source or owner (controlled by `OwnershipEntity`).
- **StackThreshold**: Applies conditional effects once the effect reaches a configurable stack threshold.
- **TargetTagRequirements**: Sets tag/query-based application/ongoing/removal requirements.

All of these extend `ForgeEffectComponent` and can be added to effect data in the inspector.

#### ForgeConditionalEffect

`AdditionalEffects` takes an array of these in each of its four slots, one per effect it applies.

**Properties:**

- **EffectData**: The `ForgeEffectData` to apply. An entry without one is skipped and reported.
- **ApplicationTarget**: Who receives it — `Target` (the default), `Source`, or `Owner`. Pointing it at `Source` is how lifesteal, recoil, and thorns are built without a custom execution.
- **SourceRequiredTags** / **SourceIgnoredTags** / **SourceTagQuery**: Conditions read from the effect's **source**, not its target. Leave them empty to always apply.
- **RemovalPolicy**: `Ignore` (the default) leaves the applied effect to live out its own duration; `RemoveOnEnd` takes it back when the applying effect ends. `RemoveOnEnd` needs both effects to be non-instant, and only means anything in `OnApplication` — the end it would take a completion effect back at is the one applying it. The field still shows in the inspector on a completion entry, since a resource cannot see which array holds it, but setting it there is rejected when the effect data is built.
- **RemoveAllStacks** / **StacksToRemove**: Only shown under `RemoveOnEnd`. `RemoveAllStacks`, on by default, removes the applied effect outright; turn it off to reveal `StacksToRemove` and take that many stacks instead.

> An effect that references itself through `AdditionalEffects`, directly or through another effect, is a loop. The cycle is cut when the effect data is built and reported through the editor's error log, but fix the configuration — the effects in the loop will not behave as authored.

#### ForgeAttributeRequirement

The two requirements components above take arrays of `ForgeAttributeRequirement` sub-resources, one per condition.

**Properties:**

- **Attribute**: The attribute to inspect. Picked from a dropdown tree of the project's scanned attribute sets, the same picker `ForgeModifier` uses, so there is nothing to type and nothing to mistype.
- **HasMinValue** / **MinValue**, **HasMaxValue** / **MaxValue**: The inclusive bounds. Godot cannot export a nullable float, so each bound is a toggle plus its value, and the value field only appears once its toggle is on.
- **ThresholdType**: Whether the bounds are raw values or percentages of the attribute's max.
- **CalculationType**: Which value to read from the attribute.
- **FinalChannel**: Only shown when CalculationType is `MagnitudeEvaluatedUpToChannel`.

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

### StatescriptAbilityBehavior

A built-in `ForgeAbilityBehavior` implementation that drives an ability's lifecycle through a Statescript graph.

**Properties:**

- `Statescript` (StatescriptGraph): The Statescript graph resource defining the ability's behavior.

**Description:**

`StatescriptAbilityBehavior` allows you to use a visual Statescript graph as the behavior for an ability, replacing the need to write a custom `IAbilityBehavior` in C#. Assign a `StatescriptGraph` resource to this behavior and set it as the `AbilityBehavior` on a `ForgeAbilityData`. At runtime, the graph is built once and cached, then each ability activation creates a new `GraphProcessor` with independent state.

If any node in the graph uses an `AbilityActivationDataResolverResource`, the behavior automatically detects the associated `IAbilityActivationDataProvider`, builds the matching `GraphAbilityBehavior<TData>`, and lets the resolver read the selected activation-data member directly from the typed payload.

**Usage:**

1. Create a `StatescriptGraph` resource in the Statescript editor.
2. Create a `ForgeAbilityData` resource.
3. Set `AbilityBehavior` to a new `StatescriptAbilityBehavior`.
4. Assign your `StatescriptGraph` to the `Statescript` property.

## Statescript Resources

For detailed documentation on Statescript concepts, see the [Statescript documentation](statescript/README.md).

### StatescriptGraph

Resource representing a complete Statescript graph definition.

**Properties:**

- `StatescriptName` (string): Display name for the graph.
- `Nodes` (Array\<StatescriptNode\>): The nodes in the graph.
- `Connections` (Array\<StatescriptConnection\>): The connections between nodes.
- `Variables` (Array\<StatescriptGraphVariable\>): Graph variable definitions.
- `ScrollOffset` (Vector2): Editor scroll position (persisted for convenience).
- `Zoom` (float): Editor zoom level (persisted for convenience).

**Usage:**

Create via the Statescript graph editor (accessible from the Forge tab) or programmatically. Assign to a `StatescriptAbilityBehavior` in a `ForgeAbilityData`.

### ForgeSharedVariableSet

Resource containing shared variable definitions for an entity. Assign to a `ForgeEntity` to define which shared variables the entity exposes at runtime.

**Properties:**

- `Variables` (Array\<ForgeSharedVariableDefinition\>): The shared variable definitions.

**Description:**

Shared variables live on the entity and are accessible by all Statescript graph instances running on that entity, providing a communication channel between abilities. For example, a "combo counter" shared variable can be read and written by multiple ability graphs on the same character.

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
