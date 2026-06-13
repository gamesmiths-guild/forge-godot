# EffectContextDataResolver

> **Type:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.EffectContextDataResolverResource`
>
> **Output Type:** `EffectApplicationContext`

Authors the optional **Context Data** input of [ApplyEffectNode](../nodes/apply-effect-node.md) and [EffectNode](../nodes/effect-node.md). It selects an `IEffectContextDataProvider` that builds custom data from the current graph state and passes it through the effect pipeline, where custom calculators and executions read it with `EffectEvaluatedData.TryGetContextData<TData>`.

## Authoring in Godot

The editor exposes a single **Provider** dropdown.

- The dropdown lists every `IEffectContextDataProvider` discovered in the project assembly, plus a **(None)** option.
- Choosing **(None)** leaves the input unbound, so effects are applied without context data.
- Choosing a provider passes the value it produces (wrapped in an `EffectApplicationContext`) to every application the node performs.
- If the provider declares authored inputs, each one renders below the dropdown as its own foldable resolver section (constant, variable, activation data, math, …), so designers can author the values the provider receives.

To make a provider appear in the dropdown, derive from `EffectContextDataProvider<TData>` and override `CreateData`:

```csharp
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Statescript;

public sealed record DamageContext(float Damage, bool IsCritical);

public sealed class DamageContextProvider : EffectContextDataProvider<DamageContext>
{
    public override DamageContext CreateData(GraphContext graphContext, EffectContextDataInputs inputs)
    {
        graphContext.TryResolve("damage", out float damage);
        graphContext.TryResolve("isCritical", out bool isCritical);
        return new DamageContext(damage, isCritical);
    }
}
```

Override `Inputs` to expose authored resolvers in the editor and read them from the `EffectContextDataInputs` bag. See [Custom Statescript Nodes](../nodes/custom-nodes.md#effect-context-data-providers) for the full provider workflow, including the authored-inputs example.

## Runtime Binding

At graph-build time, the Godot resource looks up the selected provider in the discovery registry and binds Forge's core `EffectContextDataResolver`. The provider's `CreateData` runs only when the input is resolved at application time, so graph building never invokes it. Providers are discovered via reflection and shared as cached instances, so they must be stateless.

## Related Docs

- [Resolvers Reference](README.md)
- [Core EffectContextDataResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/effect-context-data-resolver.md)
- [Custom Statescript Nodes](../nodes/custom-nodes.md#effect-context-data-providers)
- [ApplyEffectNode](../nodes/apply-effect-node.md)
- [EffectNode](../nodes/effect-node.md)
