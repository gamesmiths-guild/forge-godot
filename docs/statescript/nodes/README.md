# Statescript Nodes

This folder keeps Godot-specific node documentation alongside the canonical core Forge node docs.

Use the local pages here when a node needs Godot editor, resource, or authoring notes. Use the core docs for runtime behavior, ports, lifecycle, and C# API details.

**[Godot-only nodes](#godot-only-nodes)** — everything that reaches the scene tree, physics, input, navigation or a game's own scripts — have no core counterpart at all, so their pages here are the canonical reference for ports, settings and behavior as well as authoring.

## Core Nodes

| Category | Node | Core Docs | Godot Docs | Description |
|----------|------|-----------|------------|-------------|
| **Entry** | `EntryNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/README.md#entry-node) | — | Starts the graph and emits the initial message. |
| **Exit** | `ExitNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/README.md#exit-node) | — | Stops the graph immediately when reached. |
| **Action** | `ApplyEffectNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/action/apply-effect-node.md) | [ApplyEffectNode](apply-effect-node.md) | Applies one or more effects to one or more targets. |
| **Action** | `CancelAbilitiesNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/action/cancel-abilities-node.md) | — | Cancels active abilities on an entity, selected by the ability tags they carry. |
| **Action** | `CancelAbilityNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/action/cancel-ability-node.md) | — | Cancels the ability driving the current graph. |
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
| **Condition** | `RandomBranchNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/condition/random-branch-node.md) | — | Routes to True with a resolved probability. |
| **Condition** | `TryActivateAbilitiesByTagNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/condition/try-activate-abilities-by-tag-node.md) | — | Tries to activate abilities matching the given tags. |
| **Condition** | `TryActivateAbilityNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/condition/try-activate-ability-node.md) | — | Tries to activate an ability through its handle. |
| **Condition** | `TryCommitAbilityNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/condition/try-commit-ability-node.md) | — | Tries to commit the cost and/or cooldown of the ability driving the graph (commit-mode dropdown); True when committed. |
| **Condition** | `TryGrantAbilityAndActivateOnceNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/condition/try-grant-ability-and-activate-once-node.md) | — | Grants an ability transiently and tries to activate it once (level-override dropdown); True when the activation succeeds. Writes the still-running proc's `AbilityHandle` to an object output. |
| **Condition** | `TryRevokeAbilityNode` | [Core Doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/condition/try-revoke-ability-node.md) | — | Tries to revoke granted abilities through their handles (scope and removal-policy dropdowns); True when any was revoked. |
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

Two more editors exist for narrower reasons: the `ForEachNode` editor types its `Array` input row from the variable bound to its `Element` output (the same "the bound variable types the read" rule `SetVariableNode` uses for its target), and both loop nodes seed a fresh `Condition` slot with a constant `true` — the loop's "keep going" default — instead of the bool zero value, which would leave a newly dropped node running no iterations at all.

## Godot-only Nodes

These have no core Forge counterpart: they reach the scene tree, the physics world, input, navigation and a game's own scripts, none of which the engine-agnostic library has a concept of. Their pages here are the canonical reference — ports, settings, inputs, behavior — as well as the authoring notes.

They live under `Gamesmiths.Forge.Godot.Core.Statescript.Nodes.*`, and every one carries a `[StatescriptCategory]` that places it in a [subgroup](#node-categories-in-the-palette) of its archetype in the Add Node dialog.

### Scene

| Arch | Node | Docs | Description |
|------|------|------|-------------|
| **Action** | `InstantiateScene3DNode` / `2D` | [Scene Nodes](scene-nodes.md#the-instantiating-pair) | Instantiates a scene, parents and places it, and hands it owner and source. Fire and forget. |
| **Action** | `QueueFreeNode` | [Scene Nodes](scene-nodes.md#queue-free) | Frees a node, guarded against one already freed. |
| **Action** | `ReparentNode` | [Scene Nodes](scene-nodes.md#reparent) | Moves a node under a new parent. Stick-to-target, pick up and drop. |
| **Action** | `AddToNodeGroupNode` | [Scene Nodes](scene-nodes.md#godot-groups) | Puts a node into a Godot group permanently. |
| **Action** | `RemoveFromNodeGroupNode` | [Scene Nodes](scene-nodes.md#godot-groups) | Takes it out permanently. |
| **State** | `Scene3DNode` / `2D` | [Scene Nodes](scene-nodes.md#the-instantiating-pair) | Owns the instance: spawns on activation, frees on deactivation, with an optional lifetime and an `OnLifetimeEnd` port. |
| **State** | `NodeGroupNode` | [Scene Nodes](scene-nodes.md#godot-groups) | Holds a group membership for exactly as long as the ability, and removes only what it added. |

### Spatial

| Arch | Node | Docs | Description |
|------|------|------|-------------|
| **Action** | `SetPosition3DNode` / `2D` | [Spatial Nodes](spatial-nodes.md#instant-writers-action) | Moves an entity instantly. Blink and teleport. |
| **Action** | `SetRotation3DNode` / `2D` | [Spatial Nodes](spatial-nodes.md#instant-writers-action) | Writes a rotation instantly. 2D takes radians. |
| **Action** | `SetScale3DNode` / `2D` | [Spatial Nodes](spatial-nodes.md#instant-writers-action) | Growing zones. |
| **Action** | `SetRotationToward3DNode` / `2D` | [Spatial Nodes](spatial-nodes.md#instant-writers-action) | Turns to face a point, once. |
| **State** | `MoveTo3DNode` / `2D` | [Spatial Nodes](spatial-nodes.md#move-to) | Transform interpolation with easing and an arc. Non-solving; `OnArrived`. |
| **State** | `MoveBody3DNode` / `2D` | [Spatial Nodes](spatial-nodes.md#move-body) | The solving move. Sweeps with `MoveAndCollide`; `OnArrived` and `OnBlocked` with the blocker. |
| **State** | `RotateTo3DNode` / `2D` | [Spatial Nodes](spatial-nodes.md#rotate-to) | Turns over time to a rotation captured at activation; `OnAligned`. |
| **State** | `LookAt3DNode` / `2D` | [Spatial Nodes](spatial-nodes.md#look-at) | Keeps facing a re-resolved target, with an optional turn-rate ceiling. |

### Physics

| Arch | Node | Docs | Description |
|------|------|------|-------------|
| **Action** | `SetVelocity3DNode` / `2D` | [Physics Nodes](physics-nodes.md#velocity-and-impulses-action) | Writes a character or rigid body's velocity. Dash, knockback. |
| **Action** | `ApplyImpulse3DNode` / `2D` | [Physics Nodes](physics-nodes.md#velocity-and-impulses-action) | Rigid body impulse, with an optional offset. |
| **Action** | `SetAngularVelocity3DNode` / `2D` | [Physics Nodes](physics-nodes.md#velocity-and-impulses-action) | Rigid body spin rate. |
| **Action** | `ApplyTorqueImpulse3DNode` / `2D` | [Physics Nodes](physics-nodes.md#velocity-and-impulses-action) | The angular Apply Impulse. |
| **Action** | `SetCollisionBits3DNode` / `2D` | [Physics Nodes](physics-nodes.md#collision-bits) | Permanent layer/mask write. |
| **State** | `ForceOverride3DNode` / `2D` | [Physics Nodes](physics-nodes.md#force-override-state) | Holds a body's constant force and torque, restoring both on deactivate or abort. |
| **State** | `CollisionOverride3DNode` / `2D` | [Physics Nodes](physics-nodes.md#collision-bits) | Held layer/mask change, restoring only the bits it acted on. |
| **Condition** | `Raycast3DNode` / `2D` | [Physics Query Nodes](physics-query-nodes.md#raycast) | Routes on hit/miss and writes five hit outputs. |
| **Condition** | `Shapecast3DNode` / `2D` | [Physics Query Nodes](physics-query-nodes.md#shapecast) | A raycast with thickness; the same five outputs. |
| **State** | `Ray3DNode` / `2D` | [Physics Query Nodes](physics-query-nodes.md#monitored-casts-state) | The monitored ray: `OnHit`/`OnLost` plus hit and clear subgraphs. |
| **State** | `Sweep3DNode` / `2D` | [Physics Query Nodes](physics-query-nodes.md#monitored-casts-state) | The monitored shapecast, same port shape. |
| **State** | `Overlap3DNode` / `2D` | [Physics Query Nodes](physics-query-nodes.md#overlap) | Watches an existing area or a transient shape; per-entity `OnEntered`/`OnExited` plus occupancy subgraphs. |
| **State** | `LineOfSight3DNode` / `2D` | [Physics Query Nodes](physics-query-nodes.md#line-of-sight) | Watches the line between two points and reports the blocker. |

### Presentation

| Arch | Node | Docs | Description |
|------|------|------|-------------|
| **Action** | `PlayAnimationOneShotNode` | [Presentation Nodes](presentation-nodes.md#animation) | Starts an animation and moves on. |
| **Action** | `PlayAudioOneShotNode` | [Presentation Nodes](presentation-nodes.md#audio) | Plays an existing audio player once. |
| **State** | `PlayAnimationNode` | [Presentation Nodes](presentation-nodes.md#animation) | Owns playback; `OnFinished`, and optionally stops on an early exit. |
| **State** | `PlayAudioNode` | [Presentation Nodes](presentation-nodes.md#audio) | Holds audio for the node's lifetime. Channel hums, beam loops. |

### Input and Navigation

| Arch | Node | Docs | Description |
|------|------|------|-------------|
| **State** | `InputActionNode` | [InputActionNode](input-action-node.md) | Watches a button: `OnPressed`, `OnReleased`, and a `WhilePressed` subgraph. |
| **State** | `NavMoveTo3DNode` / `2D` | [Navigation Nodes](navigation-nodes.md) | Steers a body along a navigation path; `OnReached` / `OnFailed`. |

### Interop

| Arch | Node | Docs | Description |
|------|------|------|-------------|
| **Action** | `SetNodePropertyNode` | [Interop Nodes](interop-nodes.md#properties) | Writes a value onto a scene node's property. |
| **Action** | `SetNodeEnabledNode` | [Interop Nodes](interop-nodes.md#enabled-state) | Visibility, processing or monitoring, permanently. |
| **Action** | `CallMethodNode` | [Interop Nodes](interop-nodes.md#methods-and-signals) | Calls a method with up to two typed arguments and a typed return. |
| **Action** | `EmitSignalNode` | [Interop Nodes](interop-nodes.md#methods-and-signals) | Emits a signal with up to two typed arguments. |
| **Action** | `DebugNode` | [Interop Nodes](interop-nodes.md#debug) | Prints a resolved input value of any supported type to the Godot console. |
| **State** | `NodePropertyOverrideNode` | [Interop Nodes](interop-nodes.md#properties) | Holds a property value, restoring it on deactivate or abort. |
| **State** | `NodeEnabledOverrideNode` | [Interop Nodes](interop-nodes.md#enabled-state) | The held form of Set Node Enabled. |
| **State** | `SignalListenerNode` | [Interop Nodes](interop-nodes.md#methods-and-signals) | Watches a signal and emits `OnSignal` each time it fires. |

### The two update rails

Anything that moves a body, steers an agent, or asks the physics world a question runs on the **fixed step** — Move To, Move Body, Rotate To, Look At, Nav Move To, Force Override, Overlap, Ray, Sweep and Line Of Sight, in both dimensions. Timers, `InputActionNode`, the presentation nodes and the scene lifetimes stay on the **frame**, because their delta is wall-clock time.

The split is by what a node's delta *means*, not by what the node touches. A host that implements `IForgeEntity` itself must drive both rails; see [Ability Integration](../README.md#in-godot).

## Node Categories in the Palette

The **Add Node** dialog groups nodes by the archetype they derive from:

| Category | Derives from | Ports |
|----------|--------------|-------|
| **Action** | `ActionNode` | Execute in, Done out. |
| **Condition** | `ConditionNode` | Condition in, True / False out. |
| **State** | `StateNode<T>` | Begin / Abort in, OnActivate / OnDeactivate / OnAbort / Subgraph out, plus any ports the node adds. |
| **Flow** | `Node` | Whatever the node declares. For nodes that route messages rather than doing work — `SwitchNode` is the built-in one. |

Any concrete node type in a loaded assembly is discovered and categorized automatically, so a custom flow node deriving straight from `Node` appears under **Flow** with no plugin change. See [Custom Statescript Nodes](custom-nodes.md).

### Subgroups

`[StatescriptCategory("...")]` adds a **second level inside an archetype**. The archetypes and their colors are untouched — they group by Statescript behavior, which is the right top level — and a node without the attribute, which is every core node, stays directly under its archetype.

The Godot-only nodes use `Scene`, `Spatial`, `Physics`, `Presentation`, `Input`, `Navigation` and `Interop`. Your own nodes can declare any category name; put it on the node class:

```csharp
[StatescriptCategory("Vehicles")]
public sealed class SetThrottleNode : ActionNode { }
```

### Display names

A node's palette name is derived from its type name, with `2D` and `3D` treated as one token — `SetPosition3DNode` reads as "Set Position 3D" rather than "Set Position3 D". Apply `[StatescriptDisplayName("...")]` when the derivation reads badly.

## Configurable Port Counts

A node whose constructor argument decides how many ports it has — `SwitchNode`'s `caseCount`, `StateMachineNode`'s `stateCount` — is authored with that count on the node itself, and the editor draws exactly the ports the built graph will have. The count is persisted in `CustomData` under the constructor parameter name and passed to the constructor at build time.

Both of those nodes select their ports with an integer, so both can follow a [Statescript enum](../enums.md) instead of a bare number: bind one and the node gets a port per member, named after it.

Lowering a count removes the ports past the new end, and the connections attached to them are removed with it — as one undoable action, so a single undo brings both back.

## Adding New Node Pages

Add new node pages to this folder as they are implemented, and keep the tables above linking both the canonical core docs and any Godot-specific authoring notes. A node with a core counterpart gets a page only when Godot authoring adds something; a Godot-only node always gets one, because there is nowhere else its ports and settings are written down.

Where a family shares one authoring model — the four presentation nodes, the physics writers — document it as a single page with a section per node and link the table rows to the anchors, rather than splitting near-identical pages.

## Related Docs

- [Custom Statescript Nodes](custom-nodes.md)
- [Statescript Enums](../enums.md)
- [Physics Debug Drawing](../physics-debug-drawing.md)
- [Node Template](../templates/node-template.md)
