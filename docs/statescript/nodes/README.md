# Statescript Nodes

This folder keeps Godot-specific node documentation alongside the canonical core Forge node docs.

Use the local pages here when a node needs Godot editor, resource, or authoring notes. Use the core docs for runtime behavior, ports, lifecycle, and C# API details.

## Built-in Nodes

| Category | Node | Core Docs | Godot Docs | Description |
|----------|------|-----------|------------|-------------|
| **Entry** | `EntryNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/README.md#entry-node) | — | Starts the graph and emits the initial message. |
| **Exit** | `ExitNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/README.md#exit-node) | — | Stops the graph immediately when reached. |
| **Action** | `ApplyEffectNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/action/apply-effect-node.md) | [ApplyEffectNode](apply-effect-node.md) | Applies one or more effects to one or more targets. |
| **Action** | `DebugNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/action/README.md#built-in-action-nodes) | — | Logs a resolved input value for debugging. |
| **Action** | `SetVariableNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/action/set-variable-node.md) | — | Copies a resolved value into a graph or shared variable. |
| **Condition** | `ExpressionNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/condition/expression-node.md) | — | Branches execution based on a boolean resolver tree. |
| **State** | `EffectNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/state/effect-node.md) | [EffectNode](effect-node.md) | Keeps applied effects active while the node remains active. |
| **State** | `TimerNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/state/timer-node.md) | — | Keeps a state active for a configured duration. |

## Future Godot-specific Nodes

Add new Godot-only node pages to this folder as they are implemented, and keep the table above linking both the canonical core docs and any Godot-specific authoring notes.

## Related Docs

- [Custom Statescript Nodes](custom-nodes.md)
- [Node Template](../templates/node-template.md)
