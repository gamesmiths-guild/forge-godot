# Statescript Enums

A **Statescript enum** (`ForgeStatescriptEnum`) is a named list of values you can author integers with. It exists so that graphs that select by number — a switch, a state machine, a mode flag on a variable — read as `Attack` instead of `2`.

Enums are an **authoring-time concept only**. Nothing in the runtime knows about them: what a graph stores and evaluates is always a plain `int`. That means an enum can be added to, or taken off, existing graph data at any time without changing behavior, and no core Forge type or node has to know the feature exists.

## Creating an Enum

An enum is a **project asset**, so it is created like any other Forge resource: in the FileSystem dock, right-click a folder → **Create New → Resource…** → `ForgeStatescriptEnum`. Save it, then fill in:

- **Enum Name**: the display name. Falls back to the file name when left empty.
- **Members**: the member names, **in value order**.

Every enum asset in the project is listed in the **Enum** dropdown on the nodes and in the Enum resolver, so an enum written once is available everywhere. Adding, renaming, or moving an enum file is picked up as soon as Godot rescans.

Members are **ordinal**: the first member is `0`, the second `1`, and so on — the same model as a C# enum without explicit values.

```
res://abilities/enums/combat_stance.tres

  Enum Name : Combat Stance
  Members   : [ "Idle", "Approach", "Attack", "Recover" ]
                  0         1          2          3
```

> **Reordering renumbers.** Because members are ordinal, inserting or reordering a member changes the value of every member after it, and therefore which port or stored value it refers to. Append new members at the end unless you intend to renumber. This is the same trade-off the ports themselves have — they are index-based too — which is exactly why the two line up.

## Naming Ports: Switch and State Machine

The nodes that select a port by integer take an enum directly:

- [SwitchNode](nodes/switch-node.md): one case port per member.
- [StateMachineNode](nodes/state-machine-node.md): one state subgraph port per member.

Pick an enum from the **Enum** dropdown in the node's Settings section and the node's port count follows the enum's member count, with each port labeled by its member:

```
           Switch                    Switch  (enum: Combat Stance)
  Input ──○      ○── Case 0           Input ──○      ○── Idle
                 ○── Case 1                          ○── Approach
                 ○── Default                         ○── Attack
                                                     ○── Recover
                                                     ○── Default
```

While an enum is bound, the count spin box is read-only: the enum is the single source of truth for how many ports there are. Editing the enum asset (adding or removing a member) is picked up the next time the node is drawn, and any connections left on ports that no longer exist are removed as one undoable action.

An enum with **no members yet** cannot be followed literally — the runtime node needs at least one port — so the node falls back to a single unnamed port and says so until the enum has members.

Setting the dropdown back to **(None)** leaves the count where it is, so no ports (and no connections) are lost just by unbinding. The ports simply go back to their declared names.

The Switch node's trailing **Default** port is never part of the enum — it is the fallback for a selector outside the case range, not a case.

## Authoring Values: the Enum Resolver

Naming the ports is only half of it; the value feeding the selector should read the same way. The **Enum** resolver is a constant resolver that picks a member by name and contributes its ordinal value as a plain integer.

It is available on any input that expects an `int`, and on the wildcard inputs that accept any authorable value — so it works for the Switch/State Machine selector, but also for comparison operands, `SetVariableNode` values, and anywhere else a number is authored:

```
  Expression                       Set Variable
    Condition                        Value    [Enum ▾]  Attack (2)
      [Comparison ▾]                 Variable [stance ▾]
        Left  [Variable ▾] stance
        Op    ==
        Right [Enum ▾] Attack (2)
```

The dropdown shows `Name (value)` so the number the graph actually stores stays visible next to the name. Folded rows and the Expression node's formula preview show the member name alone.

When a node has an enum bound, a fresh selector slot starts on the Enum resolver already — picking the enum on the node is normally all the setup needed.

The resolver keeps its own enum reference, so it does not have to be the same enum the node uses (nothing stops you comparing against a different one), and a value left dangling by a removed member falls back to the enum's first member rather than silently pointing at nothing.

## What Gets Saved

| Where | Stored as |
|-------|-----------|
| Node's bound enum | A reference to the `ForgeStatescriptEnum` asset, under the editor-only `_port_enum` key in the node's `CustomData`. Stored as a reference rather than a path, so moving the enum file keeps the binding intact. |
| Node's port count | An `int` under the runtime constructor parameter name (`caseCount`, `stateCount`), read by the graph builder. |
| Enum resolver | A reference to the enum plus the selected member's `int` value. |

Because the enum reference is editor-only metadata, a graph whose enum asset is deleted still builds and runs exactly as before — the ports lose their names and fall back to `Case 0` / `State 0`, and the values stay the numbers they always were.

## Related Docs

- [SwitchNode](nodes/switch-node.md): Godot authoring notes for the switch.
- [StateMachineNode](nodes/state-machine-node.md): Godot authoring notes for the state machine.
- [Variables and Data](variables.md): Variables, scopes, and the resolver model enums plug into.
- [Nodes](nodes/README.md): Built-in node index.
