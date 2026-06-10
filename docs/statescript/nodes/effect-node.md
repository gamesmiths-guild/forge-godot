# EffectNode

> **Runtime Type:** `Gamesmiths.Forge.Statescript.Nodes.State.EffectNode`

Applies one or more effect instances, stays active while any applied instance remains active, and removes still-active instances on deactivation.

Use the core Forge docs for runtime behavior and lifecycle details. This page covers the Godot authoring details.

## Authoring in Godot

- The node has two inputs: `Effect` and `Target`.
- The `Effect` input is authored through [EffectResolver](../resolvers/effect-resolver.md), which selects one or more `ForgeEffectData` resources and carries the **Level** and **Ownership** for the produced effect.
- To reuse a single effect instance, store it once with a `SetVariable` node and re-read it on the `Effect` input through the **Variable** resolver bound to an `Effect`-typed variable.
- The input-row shape toggle switches the `Effect` binding between **single** and **array** mode.
- The `Target` input uses the standard entity resolver flow and also supports single or array bindings.
- The optional **Active Effect** output writes the produced `ActiveEffectHandle`(s) to a graph variable on activation, with the same input-following shape and `Active Effect Handle`-variable filtering as [ApplyEffectNode](apply-effect-node.md).
- The node follows the same effect/entity cross-product authoring flow as [ApplyEffectNode](apply-effect-node.md).
- The node deactivates itself automatically once every applied non-instant effect ended or was removed elsewhere. If every applied effect is instant, it deactivates in the same frame.
- The extra `OnEffectEnd` output fires only on that natural completion path. It does not fire when the node is deactivated externally and removes its own active effects during cleanup.

## Related Docs

- [Nodes Reference](README.md)
- [Core EffectNode](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/state/effect-node.md)
- [EffectResolver](../resolvers/effect-resolver.md)
- [Variables and Data](../variables.md)
