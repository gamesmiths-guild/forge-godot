# ExecuteCueNode

> **Runtime Type:** `Gamesmiths.Forge.Statescript.Nodes.Action.ExecuteCueNode`

Executes one or more one-shot cues on one or more targets.

Use the core Forge docs for runtime behavior and port details. This page covers the Godot authoring details.

## Authoring in Godot

- The node has a `Cue Tags` input and a `Target` input, plus optional `Magnitude`, `Normalized Magnitude`, `Source`, and `Custom Parameters` inputs.
- The `Cue Tags` input is authored through [TagResolver](../resolvers/tag-resolver.md): expand the tag picker and check one or more registered cue tags. The node fires every selected tag on every target.
- The `Target` input uses the standard entity resolver flow and supports single or array bindings via the input-row shape toggle.
- `Magnitude` (int), `Normalized Magnitude` (float), and `Source` (entity) are optional. When none are bound the cues are executed with no parameters; otherwise the resolved values are bundled into the cue's `CueParameters`.
- `Custom Parameters` is optional and authored through [CueCustomParametersResolver](../resolvers/cue-custom-parameters-resolver.md): pick an `ICueCustomParametersProvider` to attach a custom parameter bag (`CueParameters.CustomParameters`) the cue handler reads back by key.
- Cues are fired through each target's cues manager. The node has no output, cues are addressed entirely by tag.

## Related Docs

- [Nodes Reference](README.md)
- [Core ExecuteCueNode](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/action/execute-cue-node.md)
- [TagResolver](../resolvers/tag-resolver.md)
- [CueCustomParametersResolver](../resolvers/cue-custom-parameters-resolver.md)
- [UpdateCueNode](update-cue-node.md)
- [CueNode](cue-node.md)
