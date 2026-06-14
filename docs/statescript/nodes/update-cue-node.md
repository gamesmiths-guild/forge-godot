# UpdateCueNode

> **Runtime Type:** `Gamesmiths.Forge.Statescript.Nodes.Action.UpdateCueNode`

Updates one or more already-active cues on one or more targets. Use it to push new values to cues applied by a [CueNode](cue-node.md) without re-applying them.

Use the core Forge docs for runtime behavior and port details. This page covers the Godot authoring details.

## Authoring in Godot

- Identical authoring to [ExecuteCueNode](execute-cue-node.md): a `Cue Tags` input (the [TagResolver](../resolvers/tag-resolver.md) tag picker), a `Target` input (single/array via the shape toggle), and optional `Magnitude` / `Normalized Magnitude` / `Source` inputs plus a `Custom Parameters` input (the [CueCustomParametersResolver](../resolvers/cue-custom-parameters-resolver.md) provider dropdown).
- The only difference is that it calls the cues manager's update path, so the cue handler's `OnUpdate` runs rather than `OnExecute`.

## Related Docs

- [Nodes Reference](README.md)
- [Core UpdateCueNode](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/action/update-cue-node.md)
- [TagResolver](../resolvers/tag-resolver.md)
- [CueCustomParametersResolver](../resolvers/cue-custom-parameters-resolver.md)
- [ExecuteCueNode](execute-cue-node.md)
- [CueNode](cue-node.md)
