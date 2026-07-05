# IsValidResolver

> **Type:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.IsValidResolverResource`
>
> **Output Type:** `bool`

Authors a validity (non-null) check over an object-backed variable for condition inputs. Works with any registered object variable type: entities, effects, active effect handles, and game-registered types.

## Authoring in Godot

- Opens with a **Type** dropdown listing the registered object variable types. The **Source** picker below it offers the resolvers compatible with that type (for entities: owner/source/target, variables, and entity array accessors; for other types: object variables, plus the element resolver inside an array operation's per-element operand).
- Returns `true` when the source resolves to a non-null value, e.g. "is the stored active effect still set?".
- There is no negate option: for an "is null" check wrap it in a `Not` resolver, or when driving an `ExpressionNode` connect the `false` port.

## Runtime Binding

At graph-build time the resource builds its source through `TryBuildObjectResolver` and wraps it in the core `IsValidResolver`. A missing source, or one that does not produce an object-backed value, reports an editor error and the check resolves to a constant `false`. The selected type is editor metadata only; the runtime works on whatever the source produces.

## Related Docs

- [Resolvers Reference](README.md)
- [ObjectEqualsResolver](object-equals-resolver.md)
- [Core IsValidResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/is-valid-resolver.md)
- [Array Operations Authoring](array-operations.md)
