// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using Godot.Collections;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that binds a node's optional context-data input to an <see cref="IEffectContextDataProvider"/>.
/// </summary>
/// <remarks>
/// At graph-build time this resource looks up the selected provider and binds Forge's core
/// <see cref="EffectContextDataResolver"/>, which produces the <c>EffectApplicationContext</c> passed through the
/// effect pipeline. When the provider declares authored inputs, the stored per-input resolvers are wired into the core
/// resolver keyed by input name. When no provider is selected the input is left unbound, so effects apply without
/// context data.
/// </remarks>
[Tool]
[GlobalClass]
public partial class EffectContextDataResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "EffectContextData";

	/// <summary>
	/// Gets or sets the identifier of the <see cref="IEffectContextDataProvider"/> implementation that builds the
	/// context data. Empty means no provider is selected and the input stays unbound.
	/// </summary>
	[Export]
	public string ProviderClassName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the declared input names this resolver has authored resolvers for, parallel to
	/// <see cref="InputResolvers"/>.
	/// </summary>
	[Export]
	public Array<string> InputNames { get; set; } = [];

	/// <summary>
	/// Gets or sets the authored resolver for each entry in <see cref="InputNames"/>.
	/// </summary>
	[Export]
	public Array<StatescriptResolverResource> InputResolvers { get; set; } = [];

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		if (string.IsNullOrEmpty(ProviderClassName))
		{
			// Optional input: no provider selected means no context data is passed.
			return;
		}

		if (!EffectContextDataProviderRegistry.TryGet(ProviderClassName, out IEffectContextDataProvider provider))
		{
			GD.PushError(
				$"Statescript: Could not find effect context-data provider '{ProviderClassName}' on node '{nodeId}' " +
				$"(input {index}). The effect will be applied without context data.");
			return;
		}

		System.Collections.Generic.Dictionary<string, IPropertyResolver> inputResolvers =
			BuildInputResolvers(graph, provider);

		var propertyName = new StringKey($"__contextdata_{nodeId}_{index}");
		graph.VariableDefinitions.DefineObjectProperty(
			propertyName,
			new EffectContextDataResolver(provider, inputResolvers));
		runtimeNode.BindInput(index, propertyName);
	}

	private System.Collections.Generic.Dictionary<string, IPropertyResolver> BuildInputResolvers(
		Graph graph,
		IEffectContextDataProvider provider)
	{
		var inputResolvers = new System.Collections.Generic.Dictionary<string, IPropertyResolver>();
		IReadOnlyList<EffectContextDataInput> declaredInputs = provider.Inputs;

		for (int i = 0; i < declaredInputs.Count; i++)
		{
			EffectContextDataInput declaredInput = declaredInputs[i];
			StatescriptResolverResource? resource = FindInputResolver(declaredInput.Name);

			if (resource is null)
			{
				continue;
			}

			inputResolvers[declaredInput.Name] =
				AdaptResolverForExpectedType(resource.BuildResolver(graph), declaredInput.ValueType);
		}

		return inputResolvers;
	}

	private StatescriptResolverResource? FindInputResolver(string name)
	{
		for (int i = 0; i < InputNames.Count && i < InputResolvers.Count; i++)
		{
			if (InputNames[i] == name)
			{
				return InputResolvers[i];
			}
		}

		return null;
	}
}
