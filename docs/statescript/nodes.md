# Statescript Nodes

This page documents all node types in Statescript. For an overview of the execution model, see the [Statescript overview](README.md).

For C# API details and graph construction code examples, see the [core Nodes documentation](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes.md).

## Entry Node

The entry point of every graph. Exactly one per graph, created automatically.

**Output Ports:**

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 0 | Output | Subgraph | Emits when the graph starts. Carries the subgraph lifetime of the entire graph. |

**Behavior:**

When the graph starts, the Entry node emits a message through its output port. This is the only way execution begins. The Entry node has no input ports and does not accept incoming messages.

Because the Entry node's output is a **Subgraph port**, everything downstream is owned by the graph's lifetime. When the graph is stopped externally, the disable-subgraph signal propagates through this port to clean up all active state nodes.

---

## Exit Node

Forces the graph to stop when a message reaches it. All active state nodes are disabled, node contexts are removed, and `OnGraphCompleted` fires.

**Input Ports:**

| Index | Name | Description |
|-------|------|-------------|
| 0 | Input | Receiving a message on this port stops the entire graph. |

**Behavior:**

When a message reaches the Exit node, it calls `GraphProcessor.StopGraph()`, which disables all active state nodes, clears runtime state, and ends the ability instance. The Exit node has no output ports.

A graph may have zero or more Exit nodes. They are useful for "early termination" patterns where a condition determines the graph should end immediately.

---

## Action Nodes

Action nodes perform an **instant operation** then pass the message forward. They are the workhorses of imperative logic in Statescript.

**Input Ports:**

| Index | Name | Description |
|-------|------|-------------|
| 0 | Input | Triggers the action execution. |

**Output Ports:**

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 0 | Output | Event | Emits after the action executes. |

**Behavior:**

1. A message arrives on the input port.
2. The node's `Execute` method runs.
3. The output port emits a message.

Action nodes are stateless and instantaneous. They do not persist between frames.

### Built-in Action Nodes

#### SetVariableNode

Reads a value from an input property and writes it to an existing graph or shared variable. The bound target variable determines whether the write is treated as a value, array, reference, or reference-array assignment.

**Input Properties:**

| Index | Label | Expected Type | Description |
|-------|-------|---------------|-------------|
| 0 | Source | object | The value to read. The editor narrows this to the selected target variable's concrete type. |

**Output Variables:**

| Index | Label | Scope | Description |
|-------|-------|-------|-------------|
| 0 | Target | Graph or Shared | The variable to write to. |

---

## Condition Nodes

Condition nodes evaluate a boolean test and route the message to one of two output ports.

**Input Ports:**

| Index | Name | Description |
|-------|------|-------------|
| 0 | Input | Triggers the condition evaluation. |

**Output Ports:**

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 0 | True | Event | Emits if the test returns `true`. |
| 1 | False | Event | Emits if the test returns `false`. |

**Behavior:**

1. A message arrives on the input port.
2. The node's `Test` method evaluates.
3. Either the True or False port emits a message (never both).

### Built-in Condition Nodes

#### ExpressionNode

Evaluates a boolean input property to choose the output. This eliminates the need to create custom `ConditionNode` subclasses for data-driven conditions. Instead, compose an expression from [property resolvers](variables.md#property-resolvers) at graph construction time.

**Input Properties:**

| Index | Label | Expected Type | Description |
|-------|-------|---------------|-------------|
| 0 | Condition | bool | The boolean expression to evaluate. |

**Usage:**

The condition input can be bound to any resolver that produces a `bool`:

- A **VariableResolver** reading a bool graph variable.
- A **TagQueryResolver** checking if the entity has a tag.
- A **ComparisonResolver** comparing two values (e.g., "health > 50").
- A nested chain of resolvers for complex expressions.

This node eliminates the need to create custom Condition subclasses for data-driven conditions. Compose the logic from resolvers at graph construction time instead.

---

## State Nodes

State nodes are the heart of Statescript. Unlike action and condition nodes, state nodes **persist over time**. They activate when they receive a message, remain active across frames, and deactivate based on their internal logic. State nodes are what give Statescript its "state-based" nature and they represent ongoing conditions that own [subgraphs](subgraphs.md).

**Input Ports:**

| Index | Name | Description |
|-------|------|-------------|
| 0 | Input | Activates the state node. |
| 1 | Abort | Forcefully deactivates the state node and fires the OnAbort port. |

**Output Ports:**

| Index | Name | Type | Description |
|-------|------|------|-------------|
| 0 | OnActivate | Event | Emits immediately when the node activates. |
| 1 | OnDeactivate | Event | Emits when the node deactivates (for any reason). |
| 2 | OnAbort | Event | Emits only when the node is aborted (via the Abort input port). |
| 3 | Subgraph | Subgraph | Emits on activate; sends disable-subgraph signal on node deactivation. |
| 4+ | Custom | Event or Subgraph | Additional ports defined by subclasses (e.g., custom event or subgraph ports). |

**Behavior:**

1. A message arrives on the **Input** port → the node activates.
2. **OnActivate** and **Subgraph** ports emit messages (in that order).
3. The node is added to the graph's active state nodes list.
4. Each frame, `GraphProcessor.UpdateGraph(deltaTime)` calls the node's `OnUpdate` method.
5. When the node's internal logic completes, it deactivates:
   - **OnDeactivate** emits a regular message.
   - All **Subgraph** ports send disable-subgraph signals downstream.
   - The node is removed from the active list.
6. If the graph completes (no more active state nodes), the ability instance ends.

**Abort behavior:**

When a message arrives on the **Abort** port:
1. **OnAbort** emits a message.
2. The node deactivates (same cleanup as normal deactivation).

**Deferred actions during activation:**

If a state node's `OnActivate` handler triggers logic that would immediately deactivate the node (e.g., a condition that leads back to abort), the deactivation is **deferred** until activation is complete. This guarantees that OnActivate and Subgraph ports fire before any deactivation processing begins.

### Built-in State Nodes

#### TimerNode

Remains active for a configured duration, then deactivates. The duration is read from a bound input property, so it can be a fixed variable value, driven by an entity attribute, or any other property resolver that produces a `double`.

**Input Properties:**

| Index | Label | Expected Type | Description |
|-------|-------|---------------|-------------|
| 0 | Duration | double | Time in seconds the node should remain active. |

**Behavior:**

The timer accumulates elapsed time during `OnUpdate` calls. When elapsed time reaches or exceeds the duration, the node deactivates.

**Example:**

```
Entry → Timer(2.0)
            ├── OnActivate → [start charge animation]
            ├── OnDeactivate → [release charge, apply damage effect]
            └── Subgraph → [show charging particles]
```

When the Timer activates, it starts a charge animation and shows particles (through the Subgraph). After 2 seconds, it deactivates, triggers the damage, and the subgraph automatically cleans up the particles.

---

## Port Types

### EventPort

Carries regular messages. Does **not** propagate disable-subgraph signals. Used by Action node outputs, Condition node outputs, and State node event outputs (OnActivate, OnDeactivate, OnAbort).

### SubgraphPort

Carries **both** regular messages and disable-subgraph signals. A Subgraph port **owns** the downstream nodes connected to it: when it sends a disable signal, everything downstream is cleaned up. Used by the Entry node's output and State node Subgraph outputs. Custom state nodes can define additional Subgraph ports and control each one independently. See [Subgraphs](subgraphs.md) for details on the lifetime implications.

### InputPort

Receives messages from connected output ports and notifies the owning node.

## Graph Completion

A graph is considered complete when **no state nodes remain active**. This can happen in two ways:

1. **Natural completion**: All state nodes deactivate through their normal lifecycle (e.g., all timers expire).
2. **Forced termination**: An Exit node is reached, or `GraphProcessor.StopGraph()` is called externally (e.g., the ability is canceled).

When the graph completes, the `OnGraphCompleted` callback fires, which typically ends the ability instance.

> If a graph has no state nodes at all (only actions and conditions), it completes immediately after the initial message propagation during `StartGraph()`.
