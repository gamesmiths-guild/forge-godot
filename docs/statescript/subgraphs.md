# Subgraphs

Subgraphs are the most important concept to understand in Statescript. They establish parent-child lifetime relationships between state nodes, ensuring automatic cleanup when a parent deactivates. If you understand subgraphs, you understand how Statescript manages complexity.

For an overview of the Statescript system, see the [Statescript overview](README.md).

## What Is a Subgraph?

A **subgraph** is the set of nodes reachable through a **Subgraph port**. The ownership relationship lives at the **port level**: a Subgraph port owns the downstream nodes connected to it. When a disable-subgraph signal is sent through a port, everything activated through that port is forcefully disabled.

The default state node layout includes one Subgraph port at index 3, but custom state node subclasses can define additional Subgraph ports. Each Subgraph port owns its own independent downstream subgraph.

Because a state node owns all of its Subgraph ports, when the node deactivates, every Subgraph port receives a disable signal and every downstream subgraph is cleaned up. But while the node is active, each Subgraph port can be controlled independently: a custom node can activate one subgraph, disable it, and activate another, all without the node itself deactivating.

For example, a node that switches between two combat stances could have two custom Subgraph ports. While the node is active, it activates the subgraph for the current stance and disables the other. When the node deactivates, both subgraphs are cleaned up regardless of which one was active.

## Subgraph Port vs. OnActivate Port

Every state node has both an **OnActivate** port (Event port) and a **Subgraph** port. Both emit a regular message when the state node activates. The critical difference is what happens when a disable signal is sent:

| Port | Regular Messages | Disable-Subgraph Signal |
|------|-----------------|-------------------------|
| **OnActivate** (Event port) | Yes | Not propagated |
| **Subgraph** (Subgraph port) | Yes | Propagated to downstream nodes |

This means:

- Nodes connected to `OnActivate` are **independent siblings**. They outlive the port and must manage their own lifetime.
- Nodes connected to `Subgraph` are **owned by the port**. They are automatically disabled when the parent node deactivates.

## The Disable-Subgraph Signal

A disable-subgraph signal can be sent in two ways:

1. **Node deactivation**: When a state node deactivates (for any reason), disable signals are sent through **all** of its Subgraph ports automatically. This is the most common case.
2. **Manual port control**: A custom state node can call `EmitDisableSubgraphMessage()` on a specific Subgraph port while the node is still active, disabling just that port's downstream subgraph.

Once the signal is sent, it cascades through the subgraph:

1. **Active state nodes** downstream receive the signal: their `BeforeDisable` is called (which fires `OnDeactivate`), their own Subgraph ports propagate the signal further, and then `AfterDisable` completes the cleanup.
2. **Action and condition nodes** that previously executed receive the signal and propagate it further through their output ports, but take no other action (they have no persistent state to clean up).
3. **Already-inactive state nodes** are safely skipped. The signal only affects nodes that are currently active.

This cascade ensures complete cleanup regardless of nesting depth.

### The Entry Node's Subgraph Port

The Entry node's output port is a **Subgraph port**. This means the entire graph is itself a subgraph of the entry point. When `GraphProcessor.StopGraph()` is called, a disable-subgraph signal propagates from the Entry node, cleaning up every active state node in the graph.

## Common Patterns

### Pattern 1: Timed Behavior with Cleanup

The most common subgraph pattern: a parent timer controls the overall duration, and child nodes handle behavior during that time.

```
Entry → Timer(5s)
            ├── Subgraph → [apply buff effect]
            │                    └── Tick(1s) → [apply periodic heal]
            └── OnDeactivate → [remove buff, play end animation]
```

When the 5-second timer expires:
1. `OnDeactivate` fires → buff is removed, end animation plays.
2. Subgraph signal fires → the inner 1-second tick (and anything downstream) is forcefully stopped.

### Pattern 2: Branching Within a Subgraph

Conditions inside a subgraph can route to different state nodes. The parent's deactivation cleans up whichever branch was taken.

```
Entry → Timer(3s)
            └── Subgraph → Condition(is health > 50?)
                                ├── True → Tick(2s) → [apply strong effect]
                                └── False → Tick(1s) → [apply weak effect]
```

Only one branch activates based on the condition. When the parent timer deactivates, whichever inner tick is still active gets cleaned up automatically.

### Pattern 3: Sequential Phases

Use OnDeactivate to chain phases where each phase owns its own subgraph:

```
Entry → Timer(WindUp: 1s)
            └── OnDeactivate → Timer(Active: 3s)
                                     ├── Subgraph → [Damage Loop]
                                     └── OnDeactivate → Timer(Recovery: 2s)
```

Each phase's subgraph is independent. The damage loop is cleaned up when the Active phase ends, and the Recovery phase starts.

### Pattern 4: Nested Subgraphs

Subgraphs can be nested to any depth. Each level of nesting adds a layer of lifetime management.

```
Entry → TimerA(10s)
            └── Subgraph → TimerB(5s)
                                └── Subgraph → TimerC(2s)
                                                    └── Subgraph → [repeating action]
```

- TimerC is owned by TimerB.
- TimerB is owned by TimerA.
- When TimerA deactivates, TimerB is disabled, which in turn disables TimerC, which cleans up the repeating action.

### Pattern 5: Multiple Subgraph Children

A single state node can have multiple nodes connected to its Subgraph port. All of them activate when the parent activates, and all are disabled when the parent deactivates.

```
Entry → Timer(5s)
            └── Subgraph → Tick(1s) → [effect A]
                         → Tick(2s) → [effect B]
                         → [immediate setup action]
```

All three downstream paths activate simultaneously when the parent timer starts. When the parent deactivates, both inner ticks are cleaned up.

### Pattern 6: Custom Subgraph Port Control

Custom state nodes can define additional Subgraph ports and control them independently while the node remains active. This is one of the most powerful patterns in Statescript.

For example, a combat stance node could have two Subgraph ports, one for each stance. While active, the node switches between stances by disabling one subgraph and activating the other:

```
Entry → StanceNode
             ├── SubgraphA → [Aggressive Stance Effects]
             └── SubgraphB → [Defensive Stance Effects]
```

When the stance node starts, it activates SubgraphA. When a stance switch is triggered (e.g., via a condition in `OnUpdate`), the node calls `EmitDisableSubgraphMessage()` on SubgraphA to clean up the aggressive effects, then calls `EmitMessage()` on SubgraphB to activate the defensive effects. The node itself never deactivates during this transition.

When the stance node finally deactivates, all Subgraph ports are cleaned up automatically, disabling whichever stance subgraph was active at that point.

This pattern is possible because **ownership lives at the port level**. Each Subgraph port independently manages its downstream nodes, and the node's custom logic decides when to activate and deactivate each port.

## Important Behaviors

- **OnDeactivate always fires** regardless of deactivation reason (natural completion, abort, or disable-subgraph cascade). Use it for cleanup logic that must always run.
- **OnAbort only fires** when the Abort input port receives an explicit message. It does **not** fire during disable-subgraph cascades.
- **State nodes track activation status**: disable signals safely skip already-inactive nodes, preventing double-cleanup.
- **Deferred deactivation**: If a state node's activation logic triggers immediate deactivation (e.g., a timer with duration 0), the deactivation is deferred until activation completes. This ensures OnActivate and Subgraph ports fire before any deactivation messages.
- **Graph completion**: When the last active state node deactivates, the graph completes automatically and fires `GraphProcessor.OnGraphCompleted`.

## Common Pitfalls

### 1. Connecting to OnActivate Instead of Subgraph

**Problem:** You connect a child state node to a parent's OnActivate port instead of its Subgraph port. The child outlives the parent and the ability never ends (or ends unexpectedly).

**Solution:** Always use the Subgraph port when the child's lifetime should be bound to the parent.

### 2. Expecting OnDeactivate to Not Fire During Cleanup

**Problem:** Your OnDeactivate handler applies an effect that should only happen on natural completion, but it also runs when the ability is canceled.

**Solution:** Use a graph variable to track the deactivation reason, or create a custom port for natural completion if you need to distinguish between natural completion and forced cleanup.

### 3. Forgetting That Action-Only Graphs Complete Immediately

**Problem:** Your graph has only actions and conditions (no state nodes), and you expect it to stay active for the ability to continue processing.

**Solution:** Add at least one state node if the ability needs to persist across frames. An action-only graph completes during `StartGraph()` and immediately ends the ability instance.

### 4. Creating Loops

**Problem:** You try to connect a downstream node's output back to an upstream node's input.

**Solution:** Statescript enforces acyclic graphs at construction time. The connection is rejected with a validation error. Restructure your graph to avoid cycles and use state node transitions instead.

## Summary

- **Subgraph ports** create lifetime ownership: the port owns its downstream nodes.
- **Event ports** (OnActivate, OnDeactivate, OnAbort) are for independent events and cleanup.
- **Disable-subgraph signals** cascade through the entire downstream subgraph.
- **OnDeactivate always fires**, regardless of how deactivation was triggered.
- **Custom state nodes** can define additional Subgraph ports and control each one independently.
- **Connect child state nodes to Subgraph ports**, not Event ports like OnActivate, when you want automatic cleanup.
