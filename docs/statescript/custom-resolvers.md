# Custom Resolvers

Property resolvers provide read-only computed values that nodes can bind to as input properties. Forge ships with several [built-in resolvers](variables.md#property-resolvers) (`AttributeResolver`, `TagResolver`, `ComparisonResolver`, `VariableResolver`, `SharedVariableResolver`, `VariantResolver`, `MagnitudeResolver`), but you can create your own to expose any data source to graph nodes without writing custom node subclasses.

For core API details and code examples, see the [core Custom Resolvers documentation](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers.md).

## When to Create a Custom Resolver

Use a custom resolver when you need to expose data that the built-in resolvers don't cover. Typical scenarios:

- **Game-specific state**: Time of day, weather intensity, wave number, difficulty multiplier.
- **External system queries**: Distance to nearest enemy, number of allies in range, inventory item counts.
- **Derived calculations**: Combined values from multiple sources that don't map to a single attribute or tag.
- **Platform data**: Input device state, network latency, frame-rate-dependent values.

The advantage over reading data inside a custom node is **reusability**: a resolver can be bound to any node's input property, combined with `ComparisonResolver` for branching, or used across multiple graphs without duplicating logic.

## Architecture Overview

In Forge for Godot, a custom resolver has **three parts**:

1. **Core resolver** (`IPropertyResolver`): The runtime logic that computes the value.
2. **Resolver resource** (`StatescriptResolverResource`): A Godot resource that serializes the resolver's configuration and builds the core resolver at graph construction time.
3. **Resolver editor** (`NodeEditorProperty`): An editor control that provides the UI for configuring the resolver inside the Statescript graph editor.

All three parts are matched together by a shared **`ResolverTypeId`** string. Both the resource and the editor must return the same `ResolverTypeId`.

```
+---------------------------+     +-----------------------------+     +---------------------------+
|   IPropertyResolver       |     | StatescriptResolverResource |     | NodeEditorProperty        |
|   (core runtime logic)    |     | (Godot resource)            |     | (editor UI)               |
|                           |     |                             |     |                           |
|   Resolve(GraphContext)   |     | ResolverTypeId: "MyType"    |     | ResolverTypeId: "MyType"  |
|   ValueType: typeof(T)    |     | BindInput(graph, node...)   |     | Setup(graph, property...) |
|                           |     | BuildResolver(graph)        |     | SaveTo(property)          |
+---------------------------+     +-----------------------------+     +---------------------------+
```

## Step 1: Implement the Core Resolver

Implement `IPropertyResolver` to create the runtime resolver. This is pure C# and is the same as creating a resolver for the core library.

```csharp
using System;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

public class EntityHealthPercentResolver : IPropertyResolver
{
    private readonly StringKey _healthAttribute;
    private readonly StringKey _maxHealthAttribute;

    public EntityHealthPercentResolver(StringKey healthAttribute, StringKey maxHealthAttribute)
    {
        _healthAttribute = healthAttribute;
        _maxHealthAttribute = maxHealthAttribute;
    }

    public Type ValueType => typeof(float);

    public Variant128 Resolve(GraphContext graphContext)
    {
        if (!graphContext.TryGetActivationContext<AbilityBehaviorContext>(out var context))
        {
            return default;
        }

        if (!context.Owner.Attributes.ContainsAttribute(_healthAttribute)
            || !context.Owner.Attributes.ContainsAttribute(_maxHealthAttribute))
        {
            return default;
        }

        int health = context.Owner.Attributes[_healthAttribute].CurrentValue;
        int maxHealth = context.Owner.Attributes[_maxHealthAttribute].CurrentValue;

        if (maxHealth == 0)
        {
            return default;
        }

        return new Variant128((float)health / maxHealth);
    }
}
```

For more examples, see the [core Custom Resolvers documentation](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers.md).

## Step 2: Create the Resolver Resource

Create a Godot resource that serializes the resolver's configuration. This resource is stored inside the `StatescriptNode`'s property bindings.

**Requirements:**

- Extend `StatescriptResolverResource`.
- Add `[Tool]` and `[GlobalClass]` attributes.
- Override `ResolverTypeId` to return a unique string.
- Override `BindInput` to define the resolver on the graph and bind it to the node's input.
- Override `BuildResolver` if the resolver can be used as a nested operand (e.g., inside a `ComparisonResolver`).
- Add `[Export]` properties for any user-configurable values.

```csharp
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace MyGame.Resources;

[Tool]
[GlobalClass]
public partial class HealthPercentResolverResource : StatescriptResolverResource
{
    [Export]
    public string HealthAttribute { get; set; } = string.Empty;

    [Export]
    public string MaxHealthAttribute { get; set; } = string.Empty;

    public override string ResolverTypeId => "HealthPercent";

    public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
    {
        if (string.IsNullOrEmpty(HealthAttribute) || string.IsNullOrEmpty(MaxHealthAttribute))
        {
            return;
        }

        var propertyName = new StringKey($"__healthpct_{nodeId}_{index}");
        graph.VariableDefinitions.DefineProperty(
            propertyName,
            new EntityHealthPercentResolver(
                new StringKey(HealthAttribute),
                new StringKey(MaxHealthAttribute)));
        runtimeNode.BindInput(index, propertyName);
    }

    public override IPropertyResolver BuildResolver(Graph graph)
    {
        return new EntityHealthPercentResolver(
            new StringKey(HealthAttribute),
            new StringKey(MaxHealthAttribute));
    }
}
```

**Key points:**

- `BindInput` is called at graph build time. It registers the resolver as a property definition on the graph and binds the node input to its name.
- The `nodeId` and `index` parameters should be used to generate unique property names (e.g., `$"__healthpct_{nodeId}_{index}"`), since multiple nodes can use the same resolver type.
- `BuildResolver` is used when this resolver appears as a nested operand inside another resolver (e.g., as the left side of a `ComparisonResolver`).

## Step 3: Create the Resolver Editor

Create the editor UI control that appears inside the Statescript graph editor when the user selects your resolver type from the dropdown.

**Requirements:**

- Extend `NodeEditorProperty`.
- Add `[Tool]` attribute.
- Override `DisplayName` to set the name shown in the resolver dropdown.
- Override `ResolverTypeId` to match the resource's `ResolverTypeId`.
- Override `IsCompatibleWith` to filter which input property types this resolver supports.
- Override `Setup` to build the editor UI and restore state from an existing binding.
- Override `SaveTo` to write the current configuration to the property binding.

```csharp
#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Statescript;
using Godot;

namespace MyGame.Editor;

[Tool]
internal sealed partial class HealthPercentResolverEditor : NodeEditorProperty
{
    private LineEdit? _healthInput;
    private LineEdit? _maxHealthInput;
    private Action? _onChanged;

    public override string DisplayName => "Health %";

    public override string ResolverTypeId => "HealthPercent";

    public override bool IsCompatibleWith(Type expectedType)
    {
        // This resolver produces a float, so it's compatible with float and Variant128.
        return expectedType == typeof(float) || expectedType == typeof(Variant128);
    }

    public override void Setup(
        StatescriptGraph graph,
        StatescriptNodeProperty? property,
        Type expectedType,
        Action onChanged,
        bool isArray)
    {
        _onChanged = onChanged;

        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        var vBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        AddChild(vBox);

        // Restore values from existing binding if present.
        var healthAttr = string.Empty;
        var maxHealthAttr = string.Empty;

        if (property?.Resolver is HealthPercentResolverResource res)
        {
            healthAttr = res.HealthAttribute;
            maxHealthAttr = res.MaxHealthAttribute;
        }

        // Health attribute input.
        var healthRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        vBox.AddChild(healthRow);
        healthRow.AddChild(new Label { Text = "Health:" });
        _healthInput = new LineEdit
        {
            Text = healthAttr,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            PlaceholderText = "e.g. CombatAttributeSet.Health",
        };
        _healthInput.TextChanged += _ => _onChanged?.Invoke();
        healthRow.AddChild(_healthInput);

        // Max health attribute input.
        var maxRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        vBox.AddChild(maxRow);
        maxRow.AddChild(new Label { Text = "Max:" });
        _maxHealthInput = new LineEdit
        {
            Text = maxHealthAttr,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            PlaceholderText = "e.g. CombatAttributeSet.MaxHealth",
        };
        _maxHealthInput.TextChanged += _ => _onChanged?.Invoke();
        maxRow.AddChild(_maxHealthInput);
    }

    public override void SaveTo(StatescriptNodeProperty property)
    {
        var resource = property.Resolver as HealthPercentResolverResource
            ?? new HealthPercentResolverResource();

        resource.HealthAttribute = _healthInput?.Text ?? string.Empty;
        resource.MaxHealthAttribute = _maxHealthInput?.Text ?? string.Empty;

        property.Resolver = resource;
    }
}
#endif
```

## How Discovery Works

Both resolver resources and resolver editors are discovered automatically via reflection. You do not need to register them manually.

- **Resolver editors** (`NodeEditorProperty` subclasses): Discovered by `StatescriptResolverRegistry` from the executing assembly. Any concrete subclass of `NodeEditorProperty` is instantiated and checked for compatibility when a resolver dropdown is populated.
- **Resolver resources** (`StatescriptResolverResource` subclasses): Serialized as part of the `StatescriptNode` resource. At graph build time, the builder reads the resource's `ResolverTypeId` and calls its `BindInput` method.
- **Matching**: The `ResolverTypeId` string connects the editor to the resource. When the user selects a resolver type in the editor dropdown, the editor with that `ResolverTypeId` is shown. When saving, the editor writes its configuration to a resource with the same `ResolverTypeId`.

> **Important:** The `ResolverTypeId` must be identical between the resource and the editor. If they don't match, the editor won't be able to restore state from the serialized resource.

## Composing Resolvers

Custom resolvers compose with built-in resolvers. The most common pattern is using a custom resolver as an operand inside a `ComparisonResolver` for data-driven branching:

In the Statescript graph editor, if your custom resolver implements `BuildResolver`, it will be available as a nested operand when configuring a `Comparison` resolver's left or right side.

## Best Practices

1. **Use unique, descriptive `ResolverTypeId` values**: Avoid generic names that could collide with other resolvers. Use a project-specific prefix if needed (e.g., `"MyGame.HealthPercent"`).
2. **Handle missing data gracefully**: Always check `TryGetActivationContext` before accessing entity data. Return `default` when data is unavailable.
3. **Keep resolvers stateless**: Resolvers are shared across graph instances. Don't store mutable state on the resolver, use `GraphContext` for runtime data.
4. **Prefer built-in resolvers**: Before creating a custom resolver, check if the built-in ones cover your use case. `ComparisonResolver` with nested `AttributeResolver` and `VariantResolver` handles many common scenarios.
5. **Match editor types carefully**: `IsCompatibleWith` should reflect the resolver's actual `ValueType`. This prevents users from binding a resolver to an incompatible node input.
