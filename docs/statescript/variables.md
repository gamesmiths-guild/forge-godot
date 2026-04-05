# Variables and Data

Statescript nodes communicate through **variables** and **property resolvers**. This page covers how data flows through a graph, the different scopes and resolver types available, and how to connect graph logic to entity data.

For C# API details and code examples, see the [core Variables documentation](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/variables.md).

## Graph Variables

Graph variables are **mutable values** scoped to a single graph execution instance. They are defined at graph construction time with a name, type, and initial value. When a graph starts, each variable is initialized from its definition. Multiple executions of the same graph each get their own independent copy.

**Supported types:**

All types supported by `Variant128`: `bool`, `byte`, `sbyte`, `char`, `decimal`, `double`, `float`, `int`, `uint`, `long`, `ulong`, `short`, `ushort`, `Vector2`, `Vector3`, `Vector4`, `Plane`, `Quaternion`.

**Array variables** are also supported. These hold a fixed-length list of `Variant128` values.

### Defining Variables in Godot

In the Statescript graph editor, use the **Variables panel** to add, remove, and configure graph variables. Each variable has:

- **Name**: Used to reference the variable in property bindings.
- **Type**: The data type (e.g., Int, Float, Bool, Vector3).
- **Is Array**: Whether this is an array variable.
- **Initial Value**: The starting value when the graph begins.

### Reading and Writing Variables

Nodes interact with variables through two mechanisms:

- **Input Properties**: Declare that a node reads a named value at runtime. The value is resolved through a property resolver (which may read from a variable, an attribute, or a computed expression).
- **Output Variables**: Declare that a node writes a named variable at runtime.

For example, the `SetVariable` action node reads a value from an input property and writes it to an output variable.

## Shared Variables

Shared variables are **entity-level values** accessible by all Statescript graph instances running on the same entity. They live on the `IForgeEntity.SharedVariables` property and provide cross-ability communication.

> **Note:** [Attributes](../quick-start.md#step-3-define-an-attribute-set) and [Tags](../quick-start.md#working-with-tags) are also shared across all abilities on an entity and are often the preferred way to communicate state between graphs. Attributes handle numeric values and Tags handle boolean-like flags, and both integrate directly with the rest of Forge (effects, requirements, resolvers). Shared variables are useful when Attributes and Tags are not sufficient, for example when you need to share types beyond integer values and flags, or when you need entity-wide mutable state that doesn't map naturally to an attribute or tag.

**Example use cases:**

- A "combo counter" variable incremented by an attack ability and read by a finisher ability.
- An "ability lock" flag that prevents multiple abilities from executing simultaneously.
- A "last hit direction" vector written by a hit reaction ability and read by a dodge ability.

### Defining Shared Variables

In Godot, shared variables are defined through a `ForgeSharedVariableSet` resource assigned to the `ForgeEntity` node's `SharedVariableDefinitions` property. Each shared variable has a name, type, and initial value.

When the entity initializes, the shared variable set populates the entity's `SharedVariables` bag. All Statescript graphs running on that entity can then read and write these variables.

### Shared vs. Graph Variables

| | Graph Variables | Shared Variables |
|---|---|---|
| **Scope** | Single graph execution | Entity-wide |
| **Lifetime** | Created when graph starts, destroyed when graph ends | Created when entity initializes, persists for entity lifetime |
| **Visibility** | Only the current graph instance | All graph instances on the same entity |
| **Definition** | Variables panel in the Statescript graph editor | `ForgeSharedVariableSet` on the `ForgeEntity` |
| **Use case** | Internal graph state (e.g., counters, flags) | Cross-ability communication |

## Property Resolvers

Property resolvers provide **read-only computed values** that nodes can bind to as input properties. Each resolver implements `IPropertyResolver` and returns a `Variant128` given a `GraphContext`.

Resolvers are bound to node input properties at graph construction time. At runtime, when a node needs to read an input, the resolver computes the value from the current graph and entity state.

For how to create your own resolvers, see [Custom Resolvers](custom-resolvers.md).

### Resolution Order

When a node reads a named value through `GraphContext.TryResolve<T>()`:

1. **Graph variables** are checked first (mutable, per-execution state).
2. **Property definitions** are checked as a fallback (read-only, computed values).

This means a graph variable can "shadow" a property definition with the same name.

### Variable Resolver

Reads the current value of a graph variable by name.

**Configuration:**
- **Variable**: The name of the graph variable to read.
- **Type**: The expected value type.

**Behavior:** Looks up the named variable in the graph's runtime variables. If the variable doesn't exist, returns a default value (zero).

### Shared Variable Resolver

Reads the current value of a shared variable from the entity.

**Configuration:**
- **Variable**: The name of the shared variable to read.
- **Type**: The expected value type.

**Behavior:** Looks up the named variable in the graph context's shared variables. If no shared variables exist or the name isn't found, returns a default value.

### Variant Resolver

Holds a fixed constant value directly. Use this for hardcoded values in expressions (e.g., the right-hand side of a comparison like "health > **50**").

**Configuration:**
- **Value**: The constant value.
- **Type**: The value type.

### Attribute Resolver

Reads the current value of a specific entity attribute.

**Configuration:**
- **Attribute**: The fully qualified attribute key (e.g., `"CombatAttributeSet.Health"`).

**Behavior:** Retrieves the owner entity from the ability's `AbilityBehaviorContext` and reads the attribute's `CurrentValue`. Returns an `int`. If the graph has no activation context or the attribute doesn't exist, returns zero.

### Tag Resolver

Checks whether the owner entity has a specific gameplay tag.

**Configuration:**
- **Tag**: The tag to check for.

**Behavior:** Returns `true` if the owner entity's combined tags contain the specified tag, `false` otherwise. Requires ability activation context.

### Comparison Resolver

Compares two values using a comparison operation and returns a `bool`.

**Configuration:**
- **Left**: A nested property resolver for the left operand.
- **Operation**: The comparison (`Equal`, `NotEqual`, `LessThan`, `LessThanOrEqual`, `GreaterThan`, `GreaterThanOrEqual`).
- **Right**: A nested property resolver for the right operand.

**Behavior:** Both operands are converted to `double` for comparison, allowing any numeric property (int attributes, float variables, etc.) to be compared directly.

**Example:** To check "is health greater than 50":
- Left: `AttributeResolver("CombatAttributeSet.Health")`
- Operation: `GreaterThan`
- Right: `VariantResolver(50)`

Comparison resolvers can be nested arbitrarily. Supports nesting: operands can be other `ComparisonResolver` instances or any `IPropertyResolver`, enabling complex expressions. Use as the condition input for an `ExpressionNode` to create data-driven branching without custom code.

### Magnitude Resolver

Reads the magnitude value from the ability's activation context.

**Behavior:** Returns the `Magnitude` float from the `AbilityBehaviorContext`. This is the numeric value passed during `AbilityHandle.Activate()` or propagated from an event trigger's `EventMagnitude`. Returns `0` if no activation context is available.

### Activation Data Resolver

Reads a field from custom activation data passed when the ability was activated.

**Configuration:**
- **Provider Class**: The `IActivationDataProvider` implementation that declares the field.
- **Field Name**: The name of the field to read.
- **Field Type**: The expected type of the field.

**Behavior:** At graph build time, the resolver defines a graph variable for the field so the data binder can write to it at runtime. When the ability starts, the `GraphAbilityBehavior<TData>` writes activation data fields into graph variables, and the resolver reads them back through the standard variable system.

> **Note:** A graph supports only one activation data provider at a time. If you need multiple data types, combine them into a single provider.

For implementation details on creating activation data providers, see [Custom Nodes](custom-nodes.md#activation-data-providers).

## Data Flow Summary

When a graph executes:

1. **Graph variables** are initialized from their definitions.
2. If variable overrides are provided (e.g., from activation data), they are applied.
3. Nodes read values through **input properties**, which are resolved by **property resolvers**.
4. Resolvers query the **GraphContext** for variables, shared variables, attributes, tags, or activation context.
5. Nodes write values through **output variables** to either graph-scoped or shared-scoped variable bags.

```
+----------------------------------------------------------+
|                    GraphContext                          |
|                                                          |
|  +-------------+  +-------------+  +----------------+    |
|  | Graph Vars  |  | Shared Vars |  | Activation Ctx |    |
|  | (per-graph) |  | (per-entity)|  | (ability data) |    |
|  +------+------+  +------+------+  +-------+--------+    |
|         |                |                 |             |
|         +--------+-------+-----------------+             |
|                  |                                       |
|          +-------v-------+                               |
|          |   Resolvers   |  Attribute, Tag, Comparison,  |
|          |               |  Variable, Shared, Magnitude, |
|          |               |  Variant                      |
|          +-------+-------+                               |
|                  |                                       |
|          +-------v-------+                               |
|          |  Node Inputs  |                               |
|          +---------------+                               |
+----------------------------------------------------------+
```

## Best Practices

1. **Use shared variables for cross-ability state when attributes and tags are not sufficient**: Use graph variables for internal logic and shared variables for data that multiple abilities need to access and that doesn't map to attributes or tags.
2. **Use meaningful names**: Variable names should clearly indicate their purpose (e.g., `ComboCount`, `IsCharging`, `DashDirection`).
3. **Keep variable counts small**: Each variable adds to the initialization cost when a graph starts.
4. **Use comparison resolvers over custom conditions**: Compose boolean expressions from existing resolvers rather than writing custom `ConditionNode` subclasses.
5. **Initialize shared variables on the entity**: Always define shared variables through `ForgeSharedVariableSet` on the entity, not inside individual graphs.
6. **Use constants for fixed thresholds**: Use `VariantResolver` for constant values in comparisons, not graph variables that never change.
