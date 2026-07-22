# AbilityCostResolver

> **Type:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.AbilityCostResolverResource`
>
> **Output Type:** `int`

Reads the evaluated cost of an ability for a specific attribute, for node inputs that accept an `int`. A mana cost of 5 resolves as `-5` (the signed modifier applied to the attribute). It defaults to the ability driving the graph; provide an ability source to inspect a different ability.

## Authoring in Godot

The editor exposes a **Set** dropdown, an **Attr** dropdown, and a folded **Ability** section.

- **Set**: selects the attribute set class the cost attribute belongs to (populated from the project's attribute sets).
- **Attr**: selects the attribute within the chosen set.
- **Ability**: a nested resolver producing the `AbilityHandle` to inspect. Leave it empty to read from the ability driving the graph, or bind a [GetAbilityHandleResolver](get-ability-handle-resolver.md) (or an `AbilityHandle` variable) for cross-ability queries.

Both the set and attribute must be selected; if either is missing, the resolver pushes an error and falls back to a default `int`.

## Runtime Binding

At graph-build time, the Godot resource binds the core Forge `AbilityCostResolver`, composing the selected set and attribute into a single attribute key.

## Related Docs

- [Resolvers Reference](README.md)
- [Core AbilityCostResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/ability-cost-resolver.md)
- [GetAbilityHandleResolver](get-ability-handle-resolver.md)
