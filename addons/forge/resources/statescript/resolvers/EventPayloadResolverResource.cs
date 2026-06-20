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
/// Resolver resource that binds the raise-event node's optional payload input to an
/// <see cref="IEventPayloadProvider"/>.
/// </summary>
/// <remarks>
/// At graph-build time this resource looks up the selected provider and binds Forge's core
/// <see cref="EventPayloadResolver"/>, which produces the object attached to the event's <c>EventData.Payload</c>. When
/// the provider declares authored inputs, the stored per-input resolvers are wired into the core resolver keyed by
/// input name. When no provider is selected the input is left unbound, so events are raised without a payload.
/// </remarks>
[Tool]
[GlobalClass]
public partial class EventPayloadResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "EventPayload";

	/// <summary>
	/// Gets or sets the identifier of the <see cref="IEventPayloadProvider"/> implementation that builds the payload.
	/// Empty means no provider is selected and the input stays unbound.
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
			// Optional input: no provider selected means no payload is attached.
			return;
		}

		if (!EventPayloadProviderRegistry.TryGet(ProviderClassName, out IEventPayloadProvider provider))
		{
			GD.PushError(
				$"Statescript: Could not find event payload provider '{ProviderClassName}' on node '{nodeId}' " +
				$"(input {index}). The event will be raised without a payload.");
			return;
		}

		System.Collections.Generic.Dictionary<string, IPropertyResolver> inputResolvers =
			BuildInputResolvers(graph, provider);

		var propertyName = new StringKey($"__eventpayload_{nodeId}_{index}");
		graph.VariableDefinitions.DefineObjectProperty(
			propertyName,
			new EventPayloadResolver(provider, inputResolvers));
		runtimeNode.BindInput(index, propertyName);
	}

	private System.Collections.Generic.Dictionary<string, IPropertyResolver> BuildInputResolvers(
		Graph graph,
		IEventPayloadProvider provider)
	{
		var inputResolvers = new System.Collections.Generic.Dictionary<string, IPropertyResolver>();
		IReadOnlyList<EventPayloadInput> declaredInputs = provider.Inputs;

		for (int i = 0; i < declaredInputs.Count; i++)
		{
			EventPayloadInput declaredInput = declaredInputs[i];
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
