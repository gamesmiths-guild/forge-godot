# Input Resolvers

> **Namespace:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers`
>
> **Runtime:** `Gamesmiths.Forge.Godot.Core.Statescript.Resolvers`

These resolvers are **Godot-only** — there is no core Forge counterpart, so this page is their reference rather than a Godot-authoring supplement to one.

They read Godot input actions as values, for `ExpressionNode` gates, `ConditionMonitorNode` conditions, and any numeric or vector input. Waiting on a button is the [`InputActionNode`](../nodes/input-action-node.md) instead.

All four are unpaired — a button has no dimension.

| Resolver | Output | Rows |
|---|---|---|
| **Input Action Pressed** | `bool` | **Action**; **Mode** (`Pressed`, `JustPressed`, `JustReleased`) |
| **Input Action Strength** | `float` | **Action** | 
| **Input Axis** | `float` | **Negative**; **Positive** |
| **Input Vector 2** | `Vector2` | **Left**; **Right**; **Up**; **Down** |

**`Input Axis` is not a subtraction of two strengths.** The two actions *cancel*, which is what a subtraction does not do — holding both leaves the axis at zero rather than at whichever analog value happens to be larger.

`Input Vector 2` gives movement-direction aiming without a camera.

## The action field

The same control serves these four and the [node's Action setting](../nodes/input-action-node.md#the-action-field): free text, with a dropdown of the project's actions beside it, and an editor warning colour on a name the project does not define.

It stays free text because the project's list is a *subset* of what the runtime accepts — Godot's `ui_*` presets are valid and deliberately hidden, and a game may register actions from code — so a dropdown would be authoritative about something it is not. A resolver editor rebuilds its resource from its controls on every save, so a dropdown would also silently rewrite a stored name the next time anything in the node was touched.

## Input is client-local

These read the local machine's input state. Authoritative or networked games should sample aim and button state once at activation into [`AimActivationData`](../nodes/custom-nodes.md#built-in-activation-data-providers) rather than polling them inside a server-side graph.

## Related Docs

- [Resolvers Reference](README.md)
- [InputActionNode](../nodes/input-action-node.md)
- [Camera and Aim Resolvers](camera-and-aim.md)
