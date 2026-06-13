# ApplyEffectNode

> **Runtime Type:** `Gamesmiths.Forge.Statescript.Nodes.Action.ApplyEffectNode`

Applies one or more effect instances to one or more targets.

Use the core Forge docs for runtime behavior and port details. This page covers the Godot authoring details.

## Authoring in Godot

- The node has two required inputs, `Effect` and `Target`, plus an optional `Context Data` input.
- The `Effect` input is authored through [EffectResolver](../resolvers/effect-resolver.md), which selects one or more `ForgeEffectData` resources and carries the **Level** and **Ownership** for the produced effect.
- To reuse a single effect instance, store it once with a `SetVariable` node and re-read it on the `Effect` input through the **Variable** resolver bound to an `Effect`-typed variable.
- The input-row shape toggle switches the `Effect` binding between **single** and **array** mode.
- The `Target` input uses the standard entity resolver flow and also supports single or array bindings.
- All four effect/entity combinations are supported: single/single, single/array, array/single, and array/array.
- The optional `Context Data` input is authored through [EffectContextDataResolver](../resolvers/effect-context-data-resolver.md). Pick a provider from the dropdown to pass custom data through the effect pipeline, or leave it on **(None)** to apply effects without context data.
- The optional **Active Effect** output writes the produced `ActiveEffectHandle`(s) to a graph variable. Its shape follows the inputs (a single handle when both inputs are single, otherwise an array), and the output dropdown only lists `Active Effect Handle` variables of the matching shape. Declare one in the Variables panel to capture handles for later use; instant effects write `null` (scalar) or contribute nothing (array).

## Related Docs

- [Nodes Reference](README.md)
- [Core ApplyEffectNode](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/action/apply-effect-node.md)
- [EffectResolver](../resolvers/effect-resolver.md)
- [EffectContextDataResolver](../resolvers/effect-context-data-resolver.md)
- [Variables and Data](../variables.md)
