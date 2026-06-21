# CueCustomParametersResolver

> **Type:** `Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.CueCustomParametersResolverResource`
>
> **Output Type:** `Dictionary<StringKey, object>`

Authors the optional **Custom Parameters** input of [ExecuteCueNode](../nodes/execute-cue-node.md), [UpdateCueNode](../nodes/update-cue-node.md), and [CueNode](../nodes/cue-node.md). It selects an `ICueCustomParametersProvider` that builds the `CueParameters.CustomParameters` bag from the current graph state, which the cue handler reads back by key.

## Authoring in Godot

The editor exposes a single **Provider** dropdown.

- The dropdown lists every `ICueCustomParametersProvider` discovered in the project assembly, plus a **(None)** option.
- Choosing **(None)** leaves the input unbound, so cues fire without custom parameters.
- Choosing a provider attaches the dictionary it produces to every cue the node fires.
- If the provider declares authored inputs, each one renders below the dropdown as its own foldable resolver section (constant, variable, activation data, math, ...), so designers can author the values the provider receives.

To make a provider appear in the dropdown, derive from `CueCustomParametersProvider` and override `CreateCustomParameters`:

```csharp
using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Providers;

public sealed class DamageCueParametersProvider : CueCustomParametersProvider
{
    public override Dictionary<StringKey, object> CreateCustomParameters(
        GraphContext graphContext,
        CueCustomParameterInputs inputs)
    {
        graphContext.TryResolve("damage", out int damage);
        return new Dictionary<StringKey, object> { ["damage"] = damage };
    }
}
```

Override `Inputs` to expose authored resolvers in the editor and read them from the `CueCustomParameterInputs` bag. See [Custom Statescript Nodes](../nodes/custom-nodes.md#cue-custom-parameters-providers) for the full provider workflow, including the authored-inputs example.

## Runtime Binding

At graph-build time, the Godot resource looks up the selected provider in the discovery registry and binds Forge's core `CueCustomParametersResolver`. The provider's `CreateCustomParameters` runs only when the input is resolved at fire time, so graph building never invokes it. Providers are discovered via reflection and shared as cached instances, so they must be stateless.

## Related Docs

- [Resolvers Reference](README.md)
- [Core CueCustomParametersResolver](https://github.com/gamesmiths-guild/forge/blob/main/docs/statescript/resolvers/cue-custom-parameters-resolver.md)
- [Custom Statescript Nodes](../nodes/custom-nodes.md#cue-custom-parameters-providers)
- [ExecuteCueNode](../nodes/execute-cue-node.md)
- [UpdateCueNode](../nodes/update-cue-node.md)
- [CueNode](../nodes/cue-node.md)
