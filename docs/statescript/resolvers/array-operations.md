# Array Operations Authoring

*A Godot authoring guide for the array-operation resolver family. This is a shared how-to, not a per-resolver reference. Each operation's runtime behavior lives in its own [core resolver doc](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/README.md#array-operations), linked from the operation reference below.*

The array operations author LINQ-style pipelines for array-typed and scalar node inputs in the Godot Statescript editor: filter, sort, window, project, combine, and reduce arrays, composing other resolvers as the per-element "lambda". Every operation ships in a **value lane** (`int`/`float`/`Vector*`/... arrays) and an **object lane** (entity/reference arrays). This page covers the editor authoring model they all share.

## Authoring in Godot

### The Source

Every array operation opens with a **Source** section: a nested resolver that produces the array it works on. It accepts any array-producing resolver, an array **Variable**, a **Constant** in array mode, a composed **Array**, or **another array operation** (this is how pipelines chain). When the surrounding context does not pin an element type (for example the source of a reduction, or a `Select` projection), the **Constant** and **Array** resolvers show a **Type** dropdown so you can declare the element type inline.

### Lambda operands and iteration scope

Operations that evaluate an expression per element expose that expression as a nested operand:

- **Predicate** - `Where`, `Count`, `Any`, `All` (a `bool`). `Count` and `Any` gate it behind a **Filter by predicate** checkbox.
- **Key** - `Order By` (a numeric sort key).
- **Project** - `Select` (the projected value).

Inside these operands, and only there, the editor unlocks the **element resolvers** that stand in for the current element (the "lambda parameter"). They stay available at any nesting depth, so a `Comparison` or math resolver built inside a predicate can still read the element.

| Resolver | Reads | Notes |
|----------|-------|-------|
| **Element Value** | the current value-array element | Shows a **Type** dropdown to match the source's element type. |
| **Element Entity** | the current entity element | Is itself an entity resolver, so it feeds `Attribute`, `Tag Query`, `Is Valid`, etc.; e.g. sort by the iterated entity's health. |
| **Element Index** | the current element's zero-based index | For index-aware predicates/projections. |

The **Entity** dropdown on entity-aware resolvers (`Attribute`, `Is Valid`, `Same Entity`, `Contains Entity`, ...) also gains an **Element** option, enabled only inside a lambda operand.

### Value lane vs. object lane

The lane is chosen from the source's element type at build time, so most operations need no configuration. Element access and identity operations expose explicit entity-lane variants because their result is an entity resolver you can chain into attribute/tag reads:

- **Access:** `First` / `Last` / `Element At` (value lane) and `First Entity` / `Last Entity` / `Entity At` (entity lane).
- **Search:** `Contains` / `Index Of` (value lane) and `Contains Entity` / `Entity Index Of` (entity lane).

### Chaining - the motivating example

Because a Source accepts another array operation, the "three closest entities" pipeline is authored as `Take` → `Order By` → an entity-array variable, with the Order By **Key** set to the distance of **Element Entity**:

```
Take (Count 3)
└─ Source: Order By (Ascending)
   ├─ Source: Variable "nearbyEntities"
   └─ Key: Distance(<owner position>, Attribute("…Distance", Entity: Element))
```

## Operation Reference

Each row links to the core doc for its runtime behavior; authoring for all of them follows the model above.

| Group | Operations |
|-------|-----------|
| Element access | [First](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/first-resolver.md) · [Last](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/last-resolver.md) · [Element At](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/element-at-resolver.md) (+ entity variants) |
| Transform | [Where](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/where-resolver.md) · [Order By](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/order-by-resolver.md) · [Take](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/take-resolver.md) · [Skip](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/skip-resolver.md) · [Select](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/select-resolver.md) · [Concat](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/concat-resolver.md) · [Append](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/append-resolver.md) · [Remove At](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/remove-at-resolver.md) · [Except](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/except-resolver.md) · [Distinct](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/distinct-resolver.md) · [Reverse](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/reverse-resolver.md) |
| Reduction & aggregate | [Count](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/count-resolver.md) · [Any](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/any-resolver.md) · [All](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/all-resolver.md) · [Contains](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/contains-resolver.md) (+ entity) · [Index Of](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/index-of-resolver.md) (+ entity) · [Sum](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/sum-resolver.md) · [Average](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/average-resolver.md) · [Min Element](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/min-element-resolver.md) · [Max Element](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/max-element-resolver.md) |
| Element (lambda) | [Element Value](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/element-value-resolver.md) · [Element Entity](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/element-entity-resolver.md) · [Element Index](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/element-index-resolver.md) |

## Runtime Binding

At graph-build time each operation resource resolves its nested **Source** into either an `IArrayPropertyResolver` (value lane) or an `IObjectArrayResolver` (object lane); the object-lane operation is closed over the source's element type. Transforms define an array property (`DefineArrayProperty` / `DefineObjectArrayProperty`) and bind the node input to it; access and reductions define a scalar or entity property. Lambda operands are built once and read the current element at runtime through the graph context's element stack (see the [core element resolvers](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/element-value-resolver.md)). A missing or type-incompatible operand reports an editor error (`GD.PushError`) and falls back to a safe default instead of throwing, so an in-progress graph never breaks the build.

## Related Docs

- [Resolvers Reference](README.md)
- [Core Array Operations](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/README.md#array-operations)
- [IsValidResolver](is-valid-resolver.md)
- [ObjectEqualsResolver](object-equals-resolver.md)
- [Variables and Data](../variables.md)
