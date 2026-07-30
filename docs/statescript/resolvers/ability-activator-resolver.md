# AbilityActivatorResolver

> **Type:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.AbilityActivatorResolverResource`
>
> **Output Type:** `AbilityActivator`

Authors the optional **Activation Data** input of `TryActivateAbilityNode`, `TryActivateAbilitiesByTagNode`, and `GrantAbilityAndActivateOnceNode`. It selects an `IAbilityActivationDataProvider` that builds custom typed data from the current graph state and passes it into the activation, where the activated ability's behavior receives it through `IAbilityBehavior<TData>.OnStarted`.

> The **Activation Data** resolver is the other end of this same channel: it **reads** members *out* of the data the current ability was activated with, while this one **builds** the data a graph *sends*. Both are driven by the same `IAbilityActivationDataProvider`, so implementing one covers both. See [Ability Activation Data Providers](../nodes/custom-nodes.md#ability-activation-data-providers).

## Authoring in Godot

The editor exposes a single **Provider** dropdown.

- The dropdown lists every `IAbilityActivationDataProvider` discovered in any loaded assembly, plus a **(None)** option.
- Choosing **(None)** leaves the input unbound, so abilities are activated without custom data.
- Choosing a provider passes the value it produces to every activation the node performs.
- If the provider declares authored inputs, each one renders below the dropdown as its own foldable resolver section (constant, variable, activation data, math, …), so designers can author the values the provider receives.

To make a provider appear in the dropdown, derive from `AbilityActivationDataProvider<TData>` and override `CreateData`:

```csharp
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Providers;

public record struct DashData(float Distance, float Speed);

public sealed class DashDataProvider : AbilityActivationDataProvider<DashData>
{
    public override DashData CreateData(GraphContext graphContext, AbilityActivationDataInputs inputs)
    {
        graphContext.TryResolve("distance", out float distance);
        graphContext.TryResolve("speed", out float speed);
        return new DashData(distance, speed);
    }
}
```

Override `Members` to declare the data's members once. On this side each renders as an authored resolver, read from the `AbilityActivationDataInputs` bag; on the reading side the same entries are what the Activation Data resolver offers to bind. See [Custom Statescript Nodes](../nodes/custom-nodes.md#ability-activation-data-providers) for the full provider workflow.

## Mismatched Data

An ability whose behavior does not implement `IAbilityBehavior<TData>` for the provider's type still activates; it just starts through the untyped path and ignores the data. Nothing is skipped and nothing errors.

This matters most on `TryActivateAbilitiesByTagNode`, where a single tag usually selects several abilities that need not share an activation-data type. Use `TryActivateAbilityNode` per ability when each one needs its own payload.

## Runtime Binding

At graph-build time, the Godot resource looks up the selected provider in the discovery registry and binds Forge's core `AbilityActivatorResolver`. The provider's `CreateData` runs only when an activation actually happens, so graph building never invokes it. Providers are discovered via reflection and shared as cached instances, so they must be stateless.

## Related Docs

- [Resolvers Reference](README.md)
- [Core AbilityActivatorResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/ability-activator-resolver.md)
- [Custom Statescript Nodes](../nodes/custom-nodes.md#ability-activation-data-providers)
- [EffectContextDataResolver](effect-context-data-resolver.md) — the same pattern for effect applications
