# StateMachineNode

> **Runtime Type:** `Gamesmiths.Forge.Statescript.Nodes.State.StateMachineNode`

Keeps exactly one of several state subgraphs active, selected by a resolved integer — a graph-native state machine. The selector is re-evaluated on activation and every update tick; out-of-range selectors are clamped into the valid state range.

Use the core Forge docs for runtime behavior and lifecycle details. This page covers the Godot authoring details.

## Authoring in Godot

- **Settings → Enum:** optional, and the reason this node is much easier to read with one. The dropdown lists every [`ForgeStatescriptEnum`](../enums.md) asset in the project; pick one to get a state subgraph port per member, named after it, so the machine's states are `Idle` / `Approach` / `Attack` rather than `State 0` / `State 1` / `State 2`. Leave it on **(None)** to author the state count directly.
- **Settings → States:** how many state subgraph ports the node has. Read-only while an enum is bound, since the enum decides it. Valid range is 1–251.
- **Inputs:** the `State` input takes any `int`-producing resolver. With an enum bound, a fresh slot starts on the [Enum resolver](../enums.md#authoring-values-the-enum-resolver); in practice this input is usually a graph variable that other nodes write, which the Enum resolver can also author the initial value of.
- **Output Variables:** `Current State` (`int`) is written every time the active state changes. Bind it to a graph variable to let the rest of the graph read which state is active.

The state count is stored in the node's `CustomData` under `stateCount` (the runtime constructor parameter name) and passed to the constructor at graph-build time, so the ports drawn in the editor are exactly the ports the built graph has.

## Ports

| Port | Kind | Notes |
|------|------|-------|
| `Begin` / `Abort` | Input | Standard state node inputs. |
| `OnActivate` / `OnDeactivate` / `OnAbort` | Event output | Standard state node lifecycle events. |
| `Subgraph` | Subgraph output | The standard state node subgraph, active for the node's whole lifetime regardless of the selected state. |
| `OnStateChanged` | Event output | Emitted whenever the active state changes, including on entering the first state. |
| `State 0` … `State N-1` | Subgraph output | One per state; exactly one is active at a time. Named after the enum member when an enum is bound. |

Subgraph ports are drawn after the event ports, so the state ports appear as a block at the bottom of the node.

## Driving the Machine

A common shape is a graph variable holding the current state, written by whatever decides transitions and read by the machine:

1. Add an `int` graph variable (for example `stance`).
2. Bind the machine's `State` input to it with the **Variable** resolver.
3. Set it from anywhere in the graph with a `SetVariableNode` whose `Value` uses the **Enum** resolver, so transitions read as `stance = Attack`.
4. Optionally bind `Current State` back to a variable, and hang reactions off `OnStateChanged`.

## Changing the State Count

Lowering the count removes the state subgraph ports past the new end, and any connections attached to them go with it. The count change and the removed connections are recorded as a single undoable action, so one undo restores both. State ports are appended at the end, so raising the count never disturbs existing wiring.

## Related Docs

- [Nodes Reference](README.md)
- [Core StateMachineNode](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/state/state-machine-node.md)
- [Statescript Enums](../enums.md)
- [Subgraphs](../subgraphs.md)
- [SwitchNode](switch-node.md)
