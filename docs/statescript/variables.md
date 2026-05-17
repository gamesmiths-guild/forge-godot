# Variables and Data

Statescript nodes communicate through **variables** and **property resolvers**. This page covers how data flows through a graph, the different scopes and resolver types available, and how to connect graph logic to entity data.

For C# API details and code examples, see the [core Variables documentation](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/variables.md).

## Graph Variables

Graph variables are **mutable values** scoped to a single graph execution instance. They are defined at graph construction time with a name, type, and initial value. When a graph starts, each variable is initialized from its definition. Multiple executions of the same graph each get their own independent copy.

**Supported authoring types in Godot:**

`Bool`, `Int`, `Float`, `Vector2`, `Vector3`, `Vector4`, `Plane`, and `Quaternion`.

In the Godot editor, `Float` is the single designer-facing floating-point choice. It is backed by Forge's `double` path so it stays compatible with the broader numeric support in the core library, while still presenting a simpler authoring model.

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

In Godot, shared variables are defined through a `ForgeSharedVariableSet` resource assigned to the `ForgeEntity` node's `SharedVariableDefinitions` property. Each shared variable has a name, type, and initial value, using the same simplified authoring types as graph variables.

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

When a Godot-authored variable or resolver output needs to feed a different compatible numeric type, Forge for Godot inserts an explicit numeric coercion step during graph build instead of relying on raw `Variant128` reads. That keeps the simplified `Int`/`Float` authoring model safe for existing core resolver signatures.

For the full built-in resolver reference, see [Property Resolvers](resolvers.md). For how to create your own resolvers, see [Custom Resolvers](custom-resolvers.md).

### Resolution Order

When a node reads a named value through `GraphContext.TryResolve<T>()`:

1. **Graph variables** are checked first (mutable, per-execution state).
2. **Property definitions** are checked as a fallback (read-only, computed values).

This means a graph variable can "shadow" a property definition with the same name.

### Built-in Resolver Categories

Forge for Godot includes a large built-in resolver set covering:

- **Core data access**: Variable (graph/shared, including entity-typed variables), Variant, Attribute, Tag, Magnitude, and Activation Data.
- **Boolean expressions**: `And`, `Or`, `Not`, `Xor`, and `Comparison`.
- **Math**: Scalar math, generic numeric/vector math, interpolation, clamping, rounding, and conversion helpers.
- **Spatial math**: Vector, quaternion, plane, and transform operations.
- **Random generation**: Scalar and spatial random resolvers.

Use the [Property Resolvers](resolvers.md) reference for the full resolver table, output types, and links to the corresponding core Forge documentation.

### Activation Data Resolver

Reads a field from custom activation data passed when the ability was activated.

**Configuration:**
- **Provider Class**: The `IActivationDataProvider` implementation that declares the field and activation-data type.
- **Field Name**: The name of the field to read.
- **Field Type**: The expected type of the field.

**Behavior:** At graph build time, the resolver builds Forge's core `ActivationDataResolver`. At runtime, `StatescriptAbilityBehavior` creates the matching `GraphAbilityBehavior<TData>` and the resolver reads the selected public field or property directly from the typed activation-data payload.

> **Note:** A graph supports only one activation data provider at a time. If you need multiple data types, combine them into a single provider.

For implementation details on creating activation data providers, see [Custom Nodes](custom-nodes.md#activation-data-providers).

## Data Flow Summary

When a graph executes:

1. **Graph variables** are initialized from their definitions.
2. If variable overrides are provided (e.g., from activation data), they are applied.
3. Nodes read values through **input properties**, which are resolved by **property resolvers**.
4. Resolvers query the **GraphContext** for variables, shared variables, attributes, tags, activation data, or activation context.
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
