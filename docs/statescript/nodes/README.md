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
| **Action** | `ExecuteCueNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/action/execute-cue-node.md) | [ExecuteCueNode](execute-cue-node.md) | Executes one or more one-shot cues on one or more targets. |
| **Action** | `SetVariableNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/action/set-variable-node.md) | — | Copies a resolved value into a graph or shared variable. |
| **Action** | `UpdateCueNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/action/update-cue-node.md) | [UpdateCueNode](update-cue-node.md) | Updates one or more active cues on one or more targets. |
| **Condition** | `ExpressionNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/condition/expression-node.md) | — | Branches execution based on a boolean resolver tree. |
| **State** | `CueNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/state/cue-node.md) | [CueNode](cue-node.md) | Applies cues while active and removes them on deactivation. |
| **State** | `EffectNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/state/effect-node.md) | [EffectNode](effect-node.md) | Stays active while any effect it applied remains active and exposes an OnEffectEnd event for natural completion. |
| **State** | `TimerNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/state/timer-node.md) | — | Keeps a state active for a configured duration and exposes an OnTimerEnd event for natural completion. |

## Future Godot-specific Nodes

Add new Godot-only node pages to this folder as they are implemented, and keep the table above linking both the canonical core docs and any Godot-specific authoring notes.

## Related Docs

- [Custom Statescript Nodes](custom-nodes.md)
- [Node Template](../templates/node-template.md)
