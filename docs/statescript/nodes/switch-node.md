# SwitchNode

> **Runtime Type:** `Gamesmiths.Forge.Statescript.Nodes.Action.SwitchNode`

Routes an incoming message to one of several case ports based on a resolved integer selector. Selectors from `0` to `caseCount - 1` emit the matching case port; anything else — including a selector that fails to resolve — emits the trailing **Default** port.

Use the core Forge docs for runtime behavior. This page covers the Godot authoring details.

## Authoring in Godot

`SwitchNode` derives straight from the base `Node` rather than from one of the Action/Condition/State archetypes, so it appears under the **Flow** category in the Add Node dialog and is drawn with its own teal title bar.

- **Settings → Enum:** optional. The dropdown lists every [`ForgeStatescriptEnum`](../enums.md) asset in the project; pick one to get a case port per member, named after it. Leave it on **(None)** to author the case count directly.
- **Settings → Cases:** how many case ports the node has, not counting Default. Read-only while an enum is bound, since the enum decides it. Valid range is 1–255.
- **Inputs:** the `Selector` input takes any `int`-producing resolver — a variable, an expression, activation data. With an enum bound, a fresh slot starts on the [Enum resolver](../enums.md#authoring-values-the-enum-resolver) so the constant reads as a name.

The case count is stored in the node's `CustomData` under `caseCount` (the runtime constructor parameter name) and passed to the constructor at graph-build time, so the ports drawn in the editor are exactly the ports the built graph has.

## Ports

| Port | Kind | Notes |
|------|------|-------|
| `Input` | Input | The message to route. |
| `Case 0` … `Case N-1` | Event output | Emitted when the selector matches the case index. Named after the enum member when an enum is bound. |
| `Default` | Event output | Emitted for any selector outside the case range, and when the selector cannot be resolved. Always the last port, and never named by the enum. |

## Changing the Case Count

Because **Default** always sits after the last case, its port index moves whenever the case count changes. The editor accounts for this: a connection on Default follows the port instead of silently becoming a connection on the case that took over its index.

Lowering the count removes the case ports past the new end, and any connections attached to them go with it. The count change and the removed connections are recorded as a single undoable action, so one undo restores both.

## Related Docs

- [Nodes Reference](README.md)
- [Core SwitchNode](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/action/switch-node.md)
- [Statescript Enums](../enums.md)
- [StateMachineNode](state-machine-node.md)
