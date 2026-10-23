# InputActionNode

> **Runtime Type:** `Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State.InputActionNode`
>
> **Add Node group:** State → **Input**
>
> **Network kind:** Simulation

This is the core [InputActionNode](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/state/input-action-node.md) with the local device added: an Input Source entity without an input of its own is read from the device every frame. The core node is hidden from the Add Node dialog, since this one does everything it does.

Watches a Godot input action while active: the "wait for a button before continuing" node. Unpaired — a button has no dimension.

## Settings

| Setting | Kind | Meaning |
|---|---|---|
| **Action** | text, with a project-action dropdown beside it | The input action name to watch. |
| **Deactivate On Pressed** | checkbox, default off | Ends the node on the first press it sees, which is what makes a combo window a window. |

## Inputs

| Index | Label | Type | Notes |
|---|---|---|---|
| 0 | Input Source | `IForgeEntity` | Optional. Unbound means the ability's owner. See [below](#input-is-read-from-the-input-source). |

## Ports

| Index | Name | Kind | Emits |
|---|---|---|---|
| 4 | OnPressed | Event | On a press **this node saw**. |
| 5 | OnReleased | Event | On a release this node saw. |
| 6 | WhilePressed | Subgraph | Active for as long as the button is down. |

## A button already held when the watch begins is not a press

Every other polling node in the layer treats its first reading as a transition into whatever it found — [Overlap](physics-query-nodes.md#overlap) reports the entities already inside, [Line Of Sight](physics-query-nodes.md#line-of-sight) emits on its first check. Input cannot: the button that activated an ability is still down when the graph starts, so a first reading counted as an edge would fire `OnPressed` for a press that happened before the node existed, and a combo window opened inside a timer would trigger on a button nobody pressed inside it.

The node splits the two halves instead. **The edges are changes the node itself saw; the subgraph follows the button's state.** That is what lets an ability activated by the very button it channels on start channelling at once while still requiring a real press for `OnPressed`.

## Patterns

| Skill | Chain |
|---|---|
| Hold-to-channel | `WhilePressed` containing a `LoopTimerNode`. |
| Charged shot | `OnReleased`, with the elapsed time of a `TimerNode` scaling the magnitude. |
| Combo window | **Deactivate On Pressed** on, inside a `TimerNode` subgraph, advancing a `StateMachineNode`. |

## The action field

One control serves this setting and the four [input resolvers](../resolvers/input-resolvers.md), so an action is authored the same way wherever it appears.

The field is **free text with a dropdown of the project's actions beside it**, and it stays free text on purpose. The project's list is a *subset* of what the runtime accepts — Godot's own `ui_*` presets are valid and deliberately hidden from the list, and a game may register actions from code that no project setting knows about — so a plain dropdown would be authoritative about something it is not, and would rewrite a stored name the next time anything in the node was touched.

A name the project does not define is drawn in the editor's warning colour with a tooltip saying so, while the value itself is left exactly as typed. The cue refreshes whenever the dropdown opens, which is when a name typed before its action existed stops being unknown.

The dropdown reads the project's `input/<action>` settings — the same list, in the same order, that the Input Map tab shows — rather than the `InputMap` singleton, whose editor copy holds the *editor's* shortcuts. Feature overrides such as `input/skill_1.macos` are skipped, since they are a second setting under one action rather than a name anything can be watched by.

## Input is read from the Input Source

The button is read from the **Input Source** entity, the ability's owner when unbound, and how depends on that entity:

- **An entity with an `Input`**, such as a player with a [`ForgeInputCollector`](../../nodes.md#forgeinputcollector), is read from that input, once per physics step. The node sees what every machine simulating the entity sees, and a tap shorter than a step still shows as a press and a release, in that order. The action must be one of the project's input actions (`forge/input/actions`).
- **An entity without one** is read from the local device every frame, as `Input.IsActionPressed` reports it. Only the machine with the device can do that, so a graph that a networked game runs on its server should watch an entity with an input.

The four [input resolvers](../resolvers/input-resolvers.md) and the [mouse and camera-forward resolvers](../resolvers/camera-and-aim.md) take an Input Source the same way.

## Related Docs

- [Nodes Reference](README.md)
- [Input Resolvers](../resolvers/input-resolvers.md) — `Input Action Pressed`, `Input Action Strength`, `Input Axis`, `Input Vector 2`
- [Camera and Aim Resolvers](../resolvers/camera-and-aim.md)
