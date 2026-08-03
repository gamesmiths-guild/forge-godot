# ForEachNode

> **Runtime Type:** `Gamesmiths.Forge.Statescript.Nodes.State.ForEachNode`

Walks an array, publishing each element to a graph variable and emitting `OnIteration` for it — all on the activation frame, or spaced by an optional interval.

Use the core Forge docs for runtime behavior and lifecycle details. This page covers the Godot authoring details.

## Authoring in Godot

**Pick the element variable first.** Statescript keeps value-typed and object-backed data in separate lanes, and the node reads its source through whichever lane the bound element variable belongs to. The editor mirrors that: until you pick an **Element** variable, the `Array` input shows *"Select element variable first"* instead of a resolver row — there is no element type yet to filter the array resolvers by. This is the same pattern as [SetVariableNode](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/action/set-variable-node.md), where the target variable types the source.

- **Element (output):** lists every **non-array** graph variable, value-typed and object-backed alike. Declare an `IForgeEntity` variable to walk entities, an `Effect` variable to walk effects, an `int` variable to walk numbers. Changing it clears the `Array` binding, since resolvers valid for the old element type are not valid for the new one.
- **Array (input):** an array-shaped row typed by the element variable, so it offers only genuinely array-producing resolvers — the **Array** composite, an array variable of that element type, or any array operation (`Where`, `OrderBy`, `Select`, …). Single-value resolvers are never offered; see [Array Operations](../resolvers/array-operations.md).
- **Index (output):** lists matching `int` graph variables. Useful for magnitudes that fall off per hop, or to pick a spawn point per element.
- **Condition (input):** optional guard, evaluated once per iteration. A fresh slot is seeded with a constant `true` — the loop's "keep going" default — rather than the bool zero value, so a newly dropped node iterates.
- **Interval (input):** seconds between iterations. Left at `0`, the whole array is walked on the activation frame. Positive, the first element still lands on the activation frame and the rest are spaced out; the node stays active until the last one.

Because the node is a state node, place it as a subgraph of another state (or straight off Entry) and route `OnIteration` into the per-element work. For what follows the loop, pick the port that matches the ending you care about: **OnFinished** (the array ran out), **OnConditionFailed** (the guard cut it short), or **OnAbort** (aborted from outside). Exactly one fires; wire **OnDeactivate** instead if any ending will do.

## Type mismatches are silent, by design

Binding an element variable whose type does not match the source array is not an error: the source simply resolves nothing and the loop runs zero iterations, emitting only `OnFinished`. If a `ForEachNode` fires nothing at runtime, check that the element variable's type matches the array's element type before looking anywhere else.

## Related Docs

- [Nodes Reference](README.md)
- [Core ForEachNode](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/state/for-each-node.md)
- [Core RepeatNode](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/state/repeat-node.md) — the same loop, driven by a count
- [Array Operations](../resolvers/array-operations.md)
- [Graph Variables](../variables.md)
