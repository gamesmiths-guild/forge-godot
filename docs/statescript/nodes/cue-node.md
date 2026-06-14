# CueNode

> **Runtime Type:** `Gamesmiths.Forge.Statescript.Nodes.State.CueNode`

Applies one or more persistent cues on activation and removes them on deactivation.

Use the core Forge docs for runtime behavior and lifecycle details. This page covers the Godot authoring details.

## Authoring in Godot

- The node has a `Cue Tags` input and a `Target` input, plus optional `Magnitude`, `Normalized Magnitude`, and `Source` inputs (used for the apply).
- The `Cue Tags` input is authored through [TagResolver](../resolvers/tag-resolver.md): check one or more registered cue tags. The node applies every selected tag on every target.
- The `Target` input uses the standard entity resolver flow and supports single or array bindings via the input-row shape toggle.
- The node has no timer; it stays active until deactivated externally. Place it as a subgraph of another state node so it lives for that state's duration.
- There is no interrupted input. Whether removal counts as an interruption is derived from how the node ends: a natural shutdown (the parent subgraph ending or the graph stopping) removes cues with `interrupted: false`, while routing a signal into the node's **Abort** port removes them with `interrupted: true`.

## Related Docs

- [Nodes Reference](README.md)
- [Core CueNode](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/nodes/state/cue-node.md)
- [TagResolver](../resolvers/tag-resolver.md)
- [ExecuteCueNode](execute-cue-node.md)
- [UpdateCueNode](update-cue-node.md)
