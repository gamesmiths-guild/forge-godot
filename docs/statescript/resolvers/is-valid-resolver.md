# IsValidResolver

> **Type:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.IsValidResolverResource`
>
> **Output Type:** `bool`

Authors a validity check over an object-backed variable for condition inputs. Works with any registered object variable type: entities, effects, active effect handles, nodes, scenes, shapes, and game-registered types.

## Authoring in Godot

- Opens with a **Type** dropdown listing the registered object variable types. The **Source** picker below it offers the resolvers compatible with that type (for entities: owner/source/target, variables, and entity array accessors; for other types: object variables, plus the element resolver inside an array operation's per-element operand).
- Returns `true` when the source resolves to a value that still refers to something, e.g. "is the stored active effect still set?".
- There is no negate option: for an "is null" check wrap it in a `Not` resolver, or when driving an `ExpressionNode` connect the `false` port.

## What counts as invalid

Three things, and the third is the Godot half:

1. **Null.**
2. **A value that has stopped referring to anything.** Several core types stay non-null after they stop meaning anything — an `ActiveEffectHandle` whose effect was removed, an `AbilityHandle` whose ability was revoked, an empty `Tag`. They report themselves through core's `IValidatable`, so the resolver calls them invalid rather than merely non-null.
3. **A freed `GodotObject`.** A node a graph spawned and something else queued for deletion is a live C# reference to a dead engine object.

The third check is what the Godot layer adds. Core's `IsValid` is virtual precisely so this can be a subclass rather than a second, competing resolver — **one validity question in the editor, not two that each answer half of it.**

## Runtime Binding

At graph-build time the resource builds its source through `TryBuildObjectResolver` and wraps it in `GodotIsValidResolver`, which extends the core `IsValidResolver` with the freed-object check. A missing source, or one that does not produce an object-backed value, reports an editor error and the check resolves to a constant `false`. The selected type is editor metadata only; the runtime works on whatever the source produces.

## Related Docs

- [Resolvers Reference](README.md)
- [ObjectEqualsResolver](object-equals-resolver.md)
- [Core IsValidResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/is-valid-resolver.md)
- [Array Operations Authoring](array-operations.md)
