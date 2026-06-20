// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using Godot.Collections;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that binds the event-listener node's optional payload-output input to an
/// <see cref="IEventPayloadProvider"/> together with the graph variable each declared output is written to.
/// </summary>
/// <remarks>
/// At graph-build time this resource looks up the selected provider and binds Forge's core
/// <see cref="EventPayloadOutputResolver"/>, which produces the <c>EventPayloadWriter</c> the listener uses to
/// decompose each received payload into the bound graph variables. When no provider is selected the input is left
/// unbound, so the payload is ignored.
/// </remarks>
[Tool]
[GlobalClass]
public partial class EventPayloadOutputResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "EventPayloadOutput";

	/// <summary>
	/// Gets or sets the identifier of the <see cref="IEventPayloadProvider"/> implementation that decomposes the
	/// payload. Empty means no provider is selected and the input stays unbound.
	/// </summary>
	[Export]
	public string ProviderClassName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the declared output names that have a graph-variable binding, parallel to
	/// <see cref="VariableNames"/>.
	/// </summary>
	[Export]
	public Array<string> OutputNames { get; set; } = [];

	/// <summary>
	/// Gets or sets the graph variable each entry in <see cref="OutputNames"/> is written to.
	/// </summary>
	[Export]
	public Array<string> VariableNames { get; set; } = [];

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		if (string.IsNullOrEmpty(ProviderClassName))
		{
			// Optional input: no provider selected means the payload is not decomposed.
			return;
		}

		if (!EventPayloadProviderRegistry.TryGet(ProviderClassName, out IEventPayloadProvider provider))
		{
			GD.PushError(
				$"Statescript: Could not find event payload provider '{ProviderClassName}' on node '{nodeId}' " +
				$"(input {index}). Event payload outputs will not be written.");
			return;
		}

		var bindings = new System.Collections.Generic.Dictionary<string, EventOutputBinding>();

		for (int i = 0; i < OutputNames.Count && i < VariableNames.Count; i++)
		{
			if (string.IsNullOrEmpty(VariableNames[i]))
			{
				continue;
			}

			bindings[OutputNames[i]] = new EventOutputBinding(new StringKey(VariableNames[i]), VariableScope.Graph);
		}

		var propertyName = new StringKey($"__eventpayloadout_{nodeId}_{index}");
		graph.VariableDefinitions.DefineObjectProperty(
			propertyName,
			new EventPayloadOutputResolver(provider, bindings));
		runtimeNode.BindInput(index, propertyName);
	}
}
