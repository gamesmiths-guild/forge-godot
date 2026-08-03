# Statescript Nodes

This folder keeps Godot-specific node documentation alongside the canonical core Forge node docs.

Use the local pages here when a node needs Godot editor, resource, or authoring notes. Use the core docs for runtime behavior, ports, lifecycle, and C# API details.

## Built-in Nodes

| Category | Node | Core Docs | Godot Docs | Description |
|----------|------|-----------|------------|-------------|
| **Entry** | `EntryNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/README.md#entry-node) | — | Starts the graph and emits the initial message. |
| **Exit** | `ExitNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/README.md#exit-node) | — | Stops the graph immediately when reached. |
| **Action** | `ApplyEffectNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/action/apply-effect-node.md) | [ApplyEffectNode](apply-effect-node.md) | Applies one or more effects to one or more targets. |
| **Action** | `CancelAbilitiesNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/action/cancel-abilities-node.md) | — | Cancels active abilities on an entity, selected by the ability tags they carry. |
| **Action** | `CancelAbilityNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/action/cancel-ability-node.md) | — | Cancels the ability driving the current graph. |
| **Action** | `CommitAbilityNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/action/commit-ability-node.md) | — | Commits the cost and/or cooldown of the ability driving the graph (commit-mode dropdown). |
| **Action** | `DebugNode` | *(Godot-only)* | — | Prints a resolved input value of any supported type to the Godot console. Configured with the value type, an is-array flag, and an object type id, so it can inspect either lane. |
| **Action** | `ExecuteCueNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/action/execute-cue-node.md) | [ExecuteCueNode](execute-cue-node.md) | Executes one or more one-shot cues on one or more targets. |
| **Action** | `GrantAbilityPermanentlyNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/action/grant-ability-permanently-node.md) | — | Permanently grants an ability; writes the granted `AbilityHandle` to an object output. |
| **Action** | `RaiseEventNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/action/raise-event-node.md) | [RaiseEventNode](raise-event-node.md) | Raises an event on one or more target entities' event buses. |
| **Action** | `RemoveEffectNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/action/remove-effect-node.md) | — | Removes active effects through their handles (force-removal flag). |
| **Action** | `SetByCallerMagnitudeNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/action/set-by-caller-magnitude-node.md) | — | Sets a SetByCaller magnitude on effects, keyed by tag. |
| **Action** | `SetEffectInhibitionNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/action/set-effect-inhibition-node.md) | — | Sets the inhibition state of active effects. |
| **Action** | `SetEffectLevelNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/action/set-effect-level-node.md) | — | Levels up effects or sets their level (operation dropdown). |
| **Action** | `SetVariableNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/action/set-variable-node.md) | — | Copies a resolved value into a graph or shared variable. |
| **Action** | `UpdateCueNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/action/update-cue-node.md) | [UpdateCueNode](update-cue-node.md) | Updates one or more active cues on one or more targets. |
| **Flow** | `SwitchNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/action/switch-node.md) | [SwitchNode](switch-node.md) | Routes by an integer selector to one of N case ports, or to Default. |
| **Condition** | `ExpressionNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/condition/expression-node.md) | — | Branches execution based on a boolean resolver tree. |
| **Condition** | `GrantAbilityAndActivateOnceNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/condition/grant-ability-and-activate-once-node.md) | — | Grants an ability transiently and activates it once (level-override dropdown). |
| **Condition** | `RandomBranchNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/condition/random-branch-node.md) | — | Routes to True with a resolved probability. |
| **Condition** | `TryActivateAbilitiesByTagNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/condition/try-activate-abilities-by-tag-node.md) | — | Tries to activate abilities matching the given tags. |
| **Condition** | `TryActivateAbilityNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/condition/try-activate-ability-node.md) | — | Tries to activate an ability through its handle. |
| **State** | `AbilityEndListenerNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/state/ability-end-listener-node.md) | — | Emits when abilities end; writes the ended `AbilityHandle` to an object output. |
| **State** | `AttributeListenerNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/state/attribute-listener-node.md) | — | Emits on attribute value changes with the new value and delta; the observed attribute is chosen with a set/attribute picker. |
| **State** | `ConditionMonitorNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/state/condition-monitor-node.md) | — | Monitors a boolean condition, with true/false subgraph ports (two config flags). |
| **State** | `CueNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/state/cue-node.md) | [CueNode](cue-node.md) | Applies cues while active and removes them on deactivation. |
| **State** | `EffectLevelListenerNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/state/effect-level-listener-node.md) | — | Emits on effect level changes with the new level. |
| **State** | `EffectNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/state/effect-node.md) | [EffectNode](effect-node.md) | Stays active while any effect it applied remains active and exposes an OnEffectEnd event for natural completion. |
| **State** | `EventListenerNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/state/event-listener-node.md) | [EventListenerNode](event-listener-node.md) | Listens for events while active and emits OnEvent each time a matching event fires. |
| **State** | `ForEachNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/state/for-each-node.md) | [ForEachNode](for-each-node.md) | Walks an array, publishing each element to a variable; the bound element variable types the Array input. |
| **State** | `GrantAbilityNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/state/grant-ability-node.md) | — | Grants an ability while active; policy dropdowns + `AbilityHandle` object output. |
| **State** | `LoopTimerNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/state/loop-timer-node.md) | — | Emits an interval event every period, optionally finishing after N loops. |
| **State** | `RepeatNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/state/repeat-node.md) | — | Emits an iteration event a fixed number of times, on the activation frame or spaced by an interval; separate OnFinished / OnConditionFailed ports, and the Condition row is seeded to a constant `true`. |
| **State** | `StateMachineNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/state/state-machine-node.md) | [StateMachineNode](state-machine-node.md) | Keeps one state subgraph active by an integer selector. |
| **State** | `TagListenerNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/state/tag-listener-node.md) | — | Emits when watched tags are added/removed; writes the changed `Tag` to an object output. |
| **State** | `TimerNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/state/timer-node.md) | — | Keeps a state active for a configured duration and exposes an OnTimerEnd event for natural completion. |

Nodes with an object-lane output (the grant nodes' `AbilityHandle` output, the tag listener's `Tag` output) use a dedicated editor so the output binds to a variable of the matching object type. The attribute listener's dedicated editor additionally filters its `int` New Value / Delta output dropdowns to matching scalar variables. Nodes with constructor arguments (commit mode, effect-level operation, grant policies, monitor flags, the attribute listener's observed-attribute key) expose those as dropdowns/checkboxes/pickers that persist into the node's `CustomData`.

Two more editors exist for narrower reasons: `ForEachNode`'s types its `Array` input row from the variable bound to its `Element` output (the same "the bound variable types the read" rule `SetVariableNode` uses for its target), and both loop nodes seed a fresh `Condition` slot with a constant `true` — the loop's "keep going" default — instead of the bool zero value, which would leave a newly dropped node running no iterations at all.

## Node Categories in the Palette

The **Add Node** dialog groups nodes by the archetype they derive from:

| Category | Derives from | Ports |
|----------|--------------|-------|
| **Action** | `ActionNode` | Execute in, Done out. |
| **Condition** | `ConditionNode` | Condition in, True / False out. |
| **State** | `StateNode<T>` | Begin / Abort in, OnActivate / OnDeactivate / OnAbort / Subgraph out, plus any ports the node adds. |
| **Flow** | `Node` | Whatever the node declares. For nodes that route messages rather than doing work — `SwitchNode` is the built-in one. |

Any concrete node type in a loaded assembly is discovered and categorized automatically, so a custom flow node deriving straight from `Node` appears under **Flow** with no plugin change. See [Custom Statescript Nodes](custom-nodes.md).

## Configurable Port Counts

A node whose constructor argument decides how many ports it has — `SwitchNode`'s `caseCount`, `StateMachineNode`'s `stateCount` — is authored with that count on the node itself, and the editor draws exactly the ports the built graph will have. The count is persisted in `CustomData` under the constructor parameter name and passed to the constructor at build time.

Both of those nodes select their ports with an integer, so both can follow a [Statescript enum](../enums.md) instead of a bare number: bind one and the node gets a port per member, named after it.

Lowering a count removes the ports past the new end, and the connections attached to them are removed with it — as one undoable action, so a single undo brings both back.

## Future Godot-specific Nodes

Add new Godot-only node pages to this folder as they are implemented, and keep the table above linking both the canonical core docs and any Godot-specific authoring notes.

## Related Docs

- [Custom Statescript Nodes](custom-nodes.md)
- [Statescript Enums](../enums.md)
- [Node Template](../templates/node-template.md)
