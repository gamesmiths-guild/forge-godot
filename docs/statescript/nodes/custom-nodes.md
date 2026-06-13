# Custom Nodes

For now statescript ships with a small set of built-in nodes (`SetVariableNode`, `ExpressionNode`, `TimerNode`), but the system is designed to be extended. This page explains how to create custom Action, Condition, and State nodes for your game.

For additional code examples, see the [core Nodes documentation](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/README.md).

## Creating a Custom Action Node

Action nodes perform instant operations. To create one, inherit from `ActionNode` and override the `Execute` method.

**Steps:**

1. Create a new C# class that extends `ActionNode`.
2. Override `Description` to provide a human-readable summary (shown in editor).
3. Override `DefineParameters` if your node reads input properties or writes output variables.
4. Override `Execute` to implement the action logic.

**Example - Apply Effect Action:**

```csharp
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;

public class ApplyEffectNode : ActionNode
{
    private readonly EffectData _effectData;

    public ApplyEffectNode(EffectData effectData)
    {
        _effectData = effectData;
    }

    public override string Description => "Applies an effect to the ability's target.";

    protected override void Execute(GraphContext graphContext)
    {
        if (!graphContext.TryGetActivationContext<AbilityBehaviorContext>(out var context))
        {
            return;
        }

        if (context.Target is null)
        {
            return;
        }

        var effect = new Effect(_effectData, new EffectOwnership(context.Owner, context.Source));
        context.Target.EffectsManager.ApplyEffect(effect);
    }
}
```

> **Key points:**
> - The constructor receives node-specific data. In Godot, constructor parameters are populated from the node's `CustomData` dictionary via reflection.
> - `Execute` runs synchronously. After it returns, the output port emits automatically.
> - Access the ability context through `graphContext.TryGetActivationContext` when your node needs entity or ability data.

## Creating a Custom Condition Node

Condition nodes evaluate a boolean test. Inherit from `ConditionNode` and override the `Test` method.

**Example - Has Tag Condition:**

```csharp
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Tags;

public class HasTagNode : ConditionNode
{
    private readonly Tag _tag;

    public HasTagNode(Tag tag)
    {
        _tag = tag;
    }

    public override string Description => $"Checks if the owner has the tag '{_tag}'.";

    protected override bool Test(GraphContext graphContext)
    {
        if (!graphContext.TryGetActivationContext<AbilityBehaviorContext>(out var context))
        {
            return false;
        }

        return context.Owner.Tags.AllTags.HasTag(_tag);
    }
}
```

> **Tip:** For many condition use cases, the built-in `ExpressionNode` combined with property resolvers (`TagQueryResolver`, `ComparisonResolver`) is sufficient and avoids creating custom classes entirely.

## Creating a Custom State Node

State nodes are more complex because they persist over time and manage their own lifecycle. Inherit from `StateNode<T>` where `T` is a context class that holds per-instance runtime state.

**Steps:**

1. Create a context class that extends `StateNodeContext` (or implements `INodeContext`).
2. Create the node class that extends `StateNode<T>`.
3. Override `OnActivate` to initialize state when the node receives a message.
4. Override `OnDeactivate` to clean up state.
5. Override `OnUpdate` to advance logic each frame.
6. Call `DeactivateNode` or `DeactivateNodeAndEmitMessage` when the node should deactivate.

**Example - Cooldown State Node:**

```csharp
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;

// Per-instance context for the cooldown node.
public class CooldownNodeContext : StateNodeContext
{
    public double ElapsedTime { get; set; }
    public double Duration { get; set; }
}

public class CooldownNode : StateNode<CooldownNodeContext>
{
    public const byte DurationInput = 0;

    public override string Description => "Waits for a duration, then deactivates.";

    protected override void DefineParameters(
        List<InputProperty> inputProperties,
        List<OutputVariable> outputVariables)
    {
        inputProperties.Add(new InputProperty("Duration", typeof(double)));
    }

    protected override void OnActivate(GraphContext graphContext)
    {
        var context = graphContext.GetNodeContext<CooldownNodeContext>(NodeID);
        context.ElapsedTime = 0;

        // Read the duration from the bound input property.
        if (graphContext.TryResolve(InputProperties[DurationInput].BoundName, out double duration))
        {
            context.Duration = duration;
        }
    }

    protected override void OnDeactivate(GraphContext graphContext)
    {
        // Cleanup if needed.
    }

    protected override void OnUpdate(double deltaTime, GraphContext graphContext)
    {
        var context = graphContext.GetNodeContext<CooldownNodeContext>(NodeID);
        context.ElapsedTime += deltaTime;

        if (context.ElapsedTime >= context.Duration)
        {
            DeactivateNode(graphContext);
        }
    }
}
```

### Adding Custom Output Ports

State nodes can define additional output ports beyond the standard four (OnActivate, OnDeactivate, OnAbort, Subgraph). Custom ports can be `EventPort` instances for independent events, or `SubgraphPort` instances for lifetime-managed subgraphs.

**Example — State Node with a Custom "OnTick" Event Port:**

```csharp
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Ports;

public class TickingNodeContext : StateNodeContext
{
    public double ElapsedTime { get; set; }
    public double Interval { get; set; }
    public double TimeSinceLastTick { get; set; }
}

public class TickingNode : StateNode<TickingNodeContext>
{
    public const byte DurationInput = 0;
    public const byte IntervalInput = 1;

    // Custom event port, index starts after SubgraphPort (3).
    public const byte OnTickPort = 4;

    public override string Description => "Ticks at regular intervals, then deactivates.";

    protected override void DefineParameters(
        List<InputProperty> inputProperties,
        List<OutputVariable> outputVariables)
    {
        inputProperties.Add(new InputProperty("Duration", typeof(double)));
        inputProperties.Add(new InputProperty("Interval", typeof(double)));
    }

    protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
    {
        // Call base to get the standard ports (Input, Abort, OnActivate, OnDeactivate, OnAbort, Subgraph).
        base.DefinePorts(inputPorts, outputPorts);

        // Add custom event port. The label is what the Godot graph editor will show.
        outputPorts.Add(CreatePort<EventPort>(OnTickPort, "OnTick"));
    }

    protected override void OnActivate(GraphContext graphContext)
    {
        var ctx = graphContext.GetNodeContext<TickingNodeContext>(NodeID);
        ctx.ElapsedTime = 0;
        ctx.TimeSinceLastTick = 0;

        if (graphContext.TryResolve(InputProperties[DurationInput].BoundName, out double duration))
        {
            ctx.Interval = 1.0; // default
            graphContext.TryResolve(InputProperties[IntervalInput].BoundName, out ctx.Interval);
        }
    }

    protected override void OnDeactivate(GraphContext graphContext)
    {
    }

    protected override void OnUpdate(double deltaTime, GraphContext graphContext)
    {
        var ctx = graphContext.GetNodeContext<TickingNodeContext>(NodeID);
        ctx.ElapsedTime += deltaTime;
        ctx.TimeSinceLastTick += deltaTime;

        if (ctx.TimeSinceLastTick >= ctx.Interval)
        {
            ctx.TimeSinceLastTick -= ctx.Interval;
            EmitMessage(graphContext, OnTickPort);
        }

        if (graphContext.TryResolve(InputProperties[DurationInput].BoundName, out double duration)
            && ctx.ElapsedTime >= duration)
        {
            DeactivateNode(graphContext);
        }
    }
}
```

> **Important:** Use `DeactivateNodeAndEmitMessage` when you need to deactivate the node **and** emit custom event messages in one atomic operation. This ensures the messages fire before the node's Subgraph ports are disabled. Use `EmitMessage` for events that happen while the node remains active (like tick events).

## Node Parameters: Input Properties and Output Variables

### Input Properties

Input properties declare what data a node reads. Define them in `DefineParameters`:

```csharp
protected override void DefineParameters(
    List<InputProperty> inputProperties,
    List<OutputVariable> outputVariables)
{
    inputProperties.Add(new InputProperty("Duration", typeof(double)));
    inputProperties.Add(new InputProperty("DamageMultiplier", typeof(float)));
}
```

At runtime, read the resolved value through the `GraphContext`:

```csharp
if (graphContext.TryResolve(InputProperties[0].BoundName, out double duration))
{
    // Use duration.
}
```

The actual value source depends on which resolver is bound to the input in the graph editor.

### Output Variables

Output variables declare what data a node writes. Define them in `DefineParameters`:

```csharp
protected override void DefineParameters(
    List<InputProperty> inputProperties,
    List<OutputVariable> outputVariables)
{
    outputVariables.Add(new OutputVariable("Result", typeof(int)));
    outputVariables.Add(new OutputVariable("SharedFlag", typeof(bool), VariableScope.Shared));
}
```

At runtime, write values through the `GraphContext`:

```csharp
OutputVariable target = OutputVariables[0];
if (target.Scope == VariableScope.Shared)
{
    graphContext.SharedVariables?.SetVar(target.BoundName, result);
}
else
{
    graphContext.GraphVariables.SetVar(target.BoundName, result);
}
```

## Registering Custom Nodes in Godot

For your custom nodes to appear in the Statescript graph editor, they must be discoverable by the `StatescriptGraphBuilder`. The builder resolves node types by their fully qualified C# type name from loaded assemblies.

When you add a node in the graph editor, select your custom node type from the available options. The editor stores the `RuntimeTypeName` in the `StatescriptNode` resource. At build time, the builder instantiates the node via reflection, passing `CustomData` entries as constructor parameters.

Port labels also come from the runtime node definition. When creating ports, pass a label to `CreatePort<T>(index, label)` so custom flow and event ports appear in the graph editor with the names defined by your node, without changing any plugin code.

**Constructor parameter mapping:**

Constructor parameters are matched by name to entries in the node's `CustomData` dictionary. Parameter types that can be converted from Godot `Variant` values are supported (strings, numbers, `StringKey`, etc.).

## Activation Data Providers

For abilities that receive custom typed data on activation, you can create an `IActivationDataProvider` that declares which fields the graph can bind to:

```csharp
using Gamesmiths.Forge.Godot.Resources;

public record struct DashData(float Distance, float Speed);

public class DashDataProvider : IActivationDataProvider
{
    public Type ActivationDataType => typeof(DashData);

    public ForgeActivationDataField[] GetFields()
    {
        return
        [
            new ForgeActivationDataField("Distance", StatescriptVariableType.Float),
            new ForgeActivationDataField("Speed", StatescriptVariableType.Float),
        ];
    }
}
```

Once defined:

1. The provider appears automatically in the Activation Data resolver dropdown in the graph editor.
2. Nodes can bind input properties to the provider's fields.
3. At runtime, when the ability is activated with `DashData`, the resolver reads the selected public field or property directly from that payload.

> **Field types:** Fields may be any type `Variant128` supports (numbers and `System.Numerics` `Vector2`/`Vector3`/`Vector4`/`Quaternion`/`Plane`), plus the matching **Godot** math types (`Godot.Vector3`, etc.), which Forge for Godot converts automatically. Other types need a graph-variable binder or a custom resolver.

> **Constraint:** A graph supports only one activation data provider. If nodes already reference a provider, subsequent nodes are restricted to the same one.

## Effect Context Data Providers

This is the inverse of an activation data provider. Instead of reading values *out* of ability activation data, an effect context-data provider builds typed data *from* the current graph state and passes it *into* the effect pipeline when `ApplyEffectNode`/`EffectNode` apply an effect. Custom calculators and executions then read it with `EffectEvaluatedData.TryGetContextData<TData>`.

Derive from `EffectContextDataProvider<TData>` and override `CreateData`:

```csharp
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Statescript;

public sealed record DamageContext(float Damage, bool IsCritical);

public sealed class DamageContextProvider : EffectContextDataProvider<DamageContext>
{
    public override DamageContext CreateData(GraphContext graphContext, EffectContextDataInputs inputs)
    {
        graphContext.TryResolve("damage", out float damage);
        graphContext.TryResolve("isCritical", out bool isCritical);
        return new DamageContext(damage, isCritical);
    }
}
```

Once defined:

1. The provider appears automatically in the **Context Data** input dropdown on `ApplyEffectNode` and `EffectNode`.
2. Select it to wrap the provider's value in an `EffectApplicationContext` that is passed to every application; leave the dropdown on **(None)** to apply effects without context data.
3. At runtime, calculators and executions read the value through `EffectEvaluatedData.TryGetContextData<DamageContext>`.

### Authored inputs

To let designers author values directly on the node instead of pulling them from graph variables, declare **inputs**. Each declared input renders its own nested resolver dropdown under the provider, and the resolved values arrive through the `EffectContextDataInputs` bag:

```csharp
public sealed record DirectionContext(System.Numerics.Vector3 Direction);

public sealed class DirectionContextProvider : EffectContextDataProvider<DirectionContext>
{
    public override IReadOnlyList<EffectContextDataInput> Inputs =>
        [new EffectContextDataInput("Direction", typeof(System.Numerics.Vector3))];

    public override DirectionContext CreateData(GraphContext graphContext, EffectContextDataInputs inputs)
    {
        return new DirectionContext(inputs.Get<System.Numerics.Vector3>("Direction"));
    }
}
```

Selecting `DirectionContextProvider` shows a **Direction** section with a Vector3 resolver (constant, variable, activation data, ...). Input value types must be supported by `Variant128`. Note that graph values use `System.Numerics` math types, so convert to Godot's (`new Vector3(v.X, v.Y, v.Z)`) when your `TData` stores Godot types.

Providers are discovered via reflection and shared as cached instances, so keep them stateless. Build everything fresh from the supplied `GraphContext` and `EffectContextDataInputs` inside `CreateData`. See [EffectContextDataResolver](../resolvers/effect-context-data-resolver.md).

## Best Practices

1. **Prefer built-in nodes and resolvers**: Before creating a custom node, check if the built-in `ExpressionNode` with resolvers can achieve the same result. For custom data sources, consider creating a [custom resolver](../custom-resolvers.md) instead of a custom node.
2. **Keep nodes focused**: Each node should do one thing well. Compose complex behaviors by connecting multiple simple nodes.
3. **Always call DeactivateNode**: State nodes must eventually deactivate. A node that never deactivates will prevent the graph from completing and the ability from ending.
4. **Use OnDeactivate for cleanup**: Any resources, effects, or state set up during `OnActivate` should be cleaned up in `OnDeactivate`.
5. **Handle missing context gracefully**: Not all graphs are driven by abilities. Check `TryGetActivationContext` before accessing ability data.
6. **Use EmitMessage for ongoing events, DeactivateNodeAndEmitMessage for terminal events**: This distinction is important for correct message ordering.
7. **Test with the graph processor**: Create unit tests that build a graph, start the processor, advance time with `UpdateGraph`, and verify the expected behavior.

## See Also

- [Custom Resolvers](../custom-resolvers.md): Creating custom property resolvers for the Statescript graph editor.
- [Custom Editors](../custom-editors.md): Creating custom node and resolver editors for the graph editor UI.
