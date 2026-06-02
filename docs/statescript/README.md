# Statescript


Statescript is a state-based scripting system for defining ability behaviors in Forge. Instead of writing C# classes that implement `IAbilityBehavior`, you create a **graph** of interconnected nodes that describes how an ability executes, what conditions it checks, and how long it stays active.

In Forge for Godot, graphs are authored visually through the built-in **Statescript graph editor** in the Godot editor.

**Key benefits:**

- **Visual authoring**: Build ability logic in a node graph editor without writing code.
- **Composable**: Combine simple nodes into complex behaviors through connections.
- **Data-driven**: Nodes read their parameters from variables and property resolvers, making graphs configurable without code changes.
- **Integrated**: Statescript graphs plug directly into the Abilities system as a drop-in replacement for `IAbilityBehavior`.

For core Statescript API documentation, see the [core Forge Statescript documentation](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/README.md).

## Core Concepts

### Graphs

A **graph** is a collection of **nodes** connected by **ports**. Every graph has exactly one **Entry node** that starts execution when the ability activates. The graph may optionally have one or more **Exit nodes** that force the ability to end immediately.

A graph is a **definition** (a flyweight). At runtime, each ability activation creates its own `GraphProcessor` with independent state through a `GraphContext`, so the same graph definition can be shared across multiple ability instances.

### Execution Model

Statescript uses a hybrid execution model:

- **Declarative**: State nodes declare "this should be the case while I'm active." A Timer node declares "I should be active for 3 seconds." When the condition is no longer satisfied, the node deactivates itself.
- **Imperative**: Messages flow through the graph synchronously. When the Entry node fires, its message propagates through conditions and actions in sequence.

When the graph starts:

1. The **Entry node** emits a message through its output port.
2. That message travels through connections, reaching downstream nodes.
3. **Action nodes** execute instantly and pass the message forward.
4. **Condition nodes** evaluate and route the message to the True or False output.
5. **State nodes** activate when they receive a message and remain active over time.

Once all synchronous propagation is complete, only **state nodes** remain active. These nodes are updated each frame via `GraphProcessor.UpdateGraph(deltaTime)`. When a state node deactivates (e.g., a timer expires), it may emit messages that trigger further actions, conditions, or other state nodes.

**The graph completes when no state nodes remain active.**

### Message Propagation

Messages flow from output ports to input ports. This propagation is **synchronous within a single cascade**: when a node emits a message, all downstream nodes process it immediately, depth-first, before returning control to the emitting node.

### Node Categories

| Category | Purpose | Behavior |
|-----------|---------|----------|
| **Entry** | Starts the graph | Emits a message when the graph starts. One per graph. |
| **Exit** | Stops the graph | Stops all execution when reached. Optional. |
| **Action** | Instant operation | Executes immediately and passes the message forward. |
| **Condition** | Branches execution | Evaluates a test and routes the message to True or False. |
| **State** | Maintains active state over time | Activates on input, remains active, deactivates based on internal logic. |

For the built-in node index and links to the core per-node documentation, see [Nodes](nodes/README.md).

## Subgraphs

State nodes have **Subgraph** output ports in addition to regular Event output ports. Both emit a regular message when the state node activates, but the critical difference is what happens when a disable signal is sent:

- **Event ports (e.g., OnActivate)**: Downstream nodes are **independent** of the port's lifetime.
- **Subgraph ports**: Downstream state nodes are **owned by the port**. When a disable-subgraph signal is sent through the port, it forcefully deactivates any active state nodes reached through Subgraph connections.

When a state node deactivates, all of its Subgraph ports are automatically cleaned up. But custom state nodes can also control individual Subgraph ports independently while the node remains active, enabling patterns like switching between different active subgraphs.

For a deep dive, see [Subgraphs](subgraphs.md).

## Variables and Data

Graphs define **variables** that hold mutable state during execution. Variables are scoped to a single graph execution instance.

Nodes read data through **input properties** resolved at runtime by **property resolvers**, which can pull values from:

- **Graph variables**: Mutable values local to this graph execution.
- **Shared variables**: Entity-level values accessible by all graphs on the same entity.
- **Attributes**: Entity attribute values.
- **Tags**: Whether the entity has a specific tag.
- **Activation context**: Data passed when the ability was activated.
- **Comparisons**: Boolean expressions composed from other resolvers.
- **Constants**: Fixed values embedded in the graph.

For details, see [Variables and Data](variables.md).

## Ability Integration

Statescript integrates with the Abilities system through `GraphAbilityBehavior`:

1. When the ability **activates**, the graph starts processing.
2. Each frame, `OnUpdate(deltaTime)` advances all active state nodes.
3. When the graph **completes** or an **Exit node** is reached, the ability instance ends.
4. If the ability is **canceled**, the graph is stopped and all active nodes are disabled.

For details on the core `GraphAbilityBehavior` API, see the [core Ability Integration documentation](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/ability-integration.md).

### In Godot

In Forge for Godot, you use the **Statescript graph editor** to create graphs visually, and `StatescriptAbilityBehavior` (a Godot resource) to link them to abilities:

1. Create a `StatescriptGraph` resource.
2. Double-click it to open the Statescript editor and start designing your graph visually.
3. Create a `ForgeAbilityData` resource.
4. Set the ability's `AbilityBehavior` to a `StatescriptAbilityBehavior`.
5. Assign your `StatescriptGraph` to the behavior's `Statescript` property.

The `ForgeEntity` node automatically calls `Abilities.UpdateAbilities(delta)` in its `_Process` method, which drives graph updates. If you implement `IForgeEntity` directly, you must call this yourself.

## Loop Detection

Statescript graphs must be **acyclic**. The framework validates this at graph construction time and rejects connections that would create loops.

## Documentation

- [Nodes](nodes/README.md): Built-in node index for Forge for Godot, with Godot-specific node pages where needed.
- [Subgraphs](subgraphs.md): Deep dive into subgraph lifetime, patterns, and common pitfalls.
- [Variables and Data](variables.md): Variables, shared variables, and property resolvers.
- [Property Resolvers](resolvers/README.md): Index of the resolver set available in Forge for Godot, with local pages for Godot-specific resolver resources.
- [Custom Statescript Nodes](nodes/custom-nodes.md): How to create custom Action, Condition, and State nodes for Godot.
- [Custom Resolvers](custom-resolvers.md): Creating custom property resolvers to expose game-specific data.
- [Custom Editors](custom-editors.md): Creating custom node and resolver editors for the Statescript graph editor.
- [Core Statescript Documentation](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/README.md): API reference and C# code examples.
