# ApplyEffectNode

> **Runtime Type:** `Gamesmiths.Forge.Statescript.Nodes.Action.ApplyEffectNode`

Applies one or more effect instances to one or more targets.

Use the core Forge docs for runtime behavior and port details. This page covers the Godot authoring details.

## Authoring in Godot

- The node has just two inputs: `Effect` and `Target`.
- The `Effect` input is authored through [EffectResolver](../resolvers/effect-resolver.md), which selects one or more `ForgeEffectData` resources and carries the **Level** and **Ownership** for the produced effect.
- To reuse a single effect instance, store it once with a `SetVariable` node and re-read it on the `Effect` input through the **Variable** resolver bound to an `Effect`-typed variable.
- The input-row shape toggle switches the `Effect` binding between **single** and **array** mode.
- The `Target` input uses the standard entity resolver flow and also supports single or array bindings.
- All four effect/entity combinations are supported: single/single, single/array, array/single, and array/array.

## Related Docs

- [Nodes Reference](README.md)
- [Core ApplyEffectNode](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/action/apply-effect-node.md)
- [EffectResolver](../resolvers/effect-resolver.md)
- [Variables and Data](../variables.md)
