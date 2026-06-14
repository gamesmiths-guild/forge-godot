# TagResolver

> **Type:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.TagResolverResource`
>
> **Output Type:** `Tag` (one or more)

Authors the tag(s) for `Tag` inputs.

## Authoring in Godot

- Embeds the shared tag picker: expand it and check one or more registered tags.
- The selected tags are the tag side of a node's `tag x target` matrix, so a single selected tag behaves as the "single" case and multiple tags as the "array" case, there is no separate shape toggle on this input.

## Runtime Binding

At graph-build time the resource binds a lazy `ForgeTagArrayResolver`. The tags are materialized from the runtime tags manager (`Tag.RequestTag`) only when the input is resolved, so editor-time graph builds never touch the tags manager before it is ready. Tags that are not registered are skipped with a warning.

## Related Docs

- [Resolvers Reference](README.md)
- [ExecuteCueNode](../nodes/execute-cue-node.md)
- [UpdateCueNode](../nodes/update-cue-node.md)
- [CueNode](../nodes/cue-node.md)
