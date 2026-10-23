# Input Resolvers

> **Namespace:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers`
>
> **Runtime:** `Gamesmiths.Forge.Godot.Core.Statescript.Resolvers`

Each is a core Forge input resolver with the local device added, read when the Input Source entity has no input of its own: **Input Action Pressed** is [InputActionResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/input-action-resolver.md), **Input Action Strength** is [InputStrengthResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/input-strength-resolver.md), **Input Axis** is [InputAxisResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/input-axis-resolver.md) and **Input Vector 2** is [InputVectorResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/input-vector-resolver.md).

They read Godot input actions as values, for `ExpressionNode` gates, `ConditionMonitorNode` conditions, and any numeric or vector input. Waiting on a button is the [`InputActionNode`](../nodes/input-action-node.md) instead.

All four are unpaired — a button has no dimension.

| Resolver | Output | Rows |
|---|---|---|
| **Input Action Pressed** | `bool` | **Action**; **Mode** (`Pressed`, `JustPressed`, `JustReleased`) |
| **Input Action Strength** | `float` | **Action** | 
| **Input Axis** | `float` | **Negative**; **Positive** |
| **Input Vector 2** | `Vector2` | **Left**; **Right**; **Up**; **Down** |

**`Input Axis` is Godot's `Input.GetAxis`**: the positive action's strength minus the negative one's. Holding both cancels to zero when they are equally strong, and otherwise reports the difference, so the stronger side wins by how much it leads. That is one row instead of two strengths and a `Subtract`, and it keeps the operand order — negative first — the same as the engine's.

`Input Vector 2` gives movement-direction aiming without a camera.

## The action field

The same control serves these four and the [node's Action setting](../nodes/input-action-node.md#the-action-field): free text, with a dropdown of the project's actions beside it, and an editor warning colour on a name the project does not define.

It stays free text because the project's list is a *subset* of what the runtime accepts — Godot's `ui_*` presets are valid and deliberately hidden, and a game may register actions from code — so a dropdown would be authoritative about something it is not. A resolver editor rebuilds its resource from its controls on every save, so a dropdown would also silently rewrite a stored name the next time anything in the node was touched.

## Input Source

Each of the four takes an **Input Source** entity, the ability's owner when left on its default, and reads it the way the [`InputActionNode`](../nodes/input-action-node.md#input-is-read-from-the-input-source) does:

- **From an entity with an `Input`**, the value is the current physics step's, the same on every machine. `Just Pressed` and `Just Released` hold for the whole step. Strengths, axes and vectors are analog only for actions that belong to one of the project's input vectors (`forge/input/vectors`); a vector's four actions give that vector exactly, and any other action reads as fully on while held.
- **From an entity without one**, the value comes from the local device, as before.

## Related Docs

- [Resolvers Reference](README.md)
- [InputActionNode](../nodes/input-action-node.md)
- [Camera and Aim Resolvers](camera-and-aim.md)
