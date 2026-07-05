# ObjectEqualsResolver

> **Type:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.ObjectEqualsResolverResource`
>
> **Output Type:** `bool`

Authors a reference-identity check between two object-backed variables for condition inputs. Works with any registered object variable type: entities, effects, active effect handles, and game-registered types.

## Authoring in Godot

- Opens with a **Type** dropdown listing the registered object variable types. The **Left** and **Right** pickers below it offer the resolvers compatible with that type (for entities: owner/source/target, variables, and entity array accessors; for other types: object variables, plus the element resolver inside an array operation's per-element operand).
- Returns `true` when both operands resolve to the same instance (reference identity), e.g. "was this effect applied to the same target we stored?".

## Runtime Binding

At graph-build time the resource builds each operand through `TryBuildObjectResolver` and wraps them in the core `ObjectEqualsResolver`. A missing operand, or one that does not produce an object-backed value, reports an editor error and the check resolves to a constant `false`. The selected type is editor metadata only; the runtime works on whatever the operands produce.

## Related Docs

- [Resolvers Reference](README.md)
- [IsValidResolver](is-valid-resolver.md)
- [Core ObjectEqualsResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/object-equals-resolver.md)
- [Array Operations Authoring](array-operations.md)
