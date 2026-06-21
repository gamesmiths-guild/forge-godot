// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Statescript.Providers;
using Godot;
using Godot.Collections;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that binds a cue node's optional custom-parameters input to an
/// <see cref="ICueCustomParametersProvider"/>.
/// </summary>
/// <remarks>
/// At graph-build time this resource looks up the selected provider and binds Forge's core
/// <see cref="CueCustomParametersResolver"/>, which produces the <c>Dictionary&lt;StringKey, object&gt;</c> attached to
/// the cue's <c>CueParameters.CustomParameters</c>. When the provider declares authored inputs, the stored per-input
/// resolvers are wired into the core resolver keyed by input name. When no provider is selected the input is left
/// unbound, so cues fire without custom parameters.
/// </remarks>
[Tool]
[GlobalClass]
public partial class CueCustomParametersResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "CueCustomParameters";

	/// <summary>
	/// Gets or sets the identifier of the <see cref="ICueCustomParametersProvider"/> implementation that builds the
	/// custom parameters. Empty means no provider is selected and the input stays unbound.
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
			// Optional input: no provider selected means no custom parameters are passed.
			return;
		}

		if (!CueCustomParametersProviderRegistry.TryGet(ProviderClassName, out ICueCustomParametersProvider provider))
		{
			GD.PushError(
				$"Statescript: Could not find cue custom-parameters provider '{ProviderClassName}' on node " +
				$"'{nodeId}' (input {index}). The cue will be fired without custom parameters.");
			return;
		}

		System.Collections.Generic.Dictionary<string, IPropertyResolver> inputResolvers =
			BuildInputResolvers(graph, provider);

		var propertyName = new StringKey($"__cuecustomparams_{nodeId}_{index}");
		graph.VariableDefinitions.DefineObjectProperty(
			propertyName,
			new CueCustomParametersResolver(provider, inputResolvers));
		runtimeNode.BindInput(index, propertyName);
	}

	private System.Collections.Generic.Dictionary<string, IPropertyResolver> BuildInputResolvers(
		Graph graph,
		ICueCustomParametersProvider provider)
	{
		var inputResolvers = new System.Collections.Generic.Dictionary<string, IPropertyResolver>();
		IReadOnlyList<CueCustomParameterInput> declaredInputs = provider.Inputs;

		for (int i = 0; i < declaredInputs.Count; i++)
		{
			CueCustomParameterInput declaredInput = declaredInputs[i];
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
