// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that composes an <see cref="Effects.EffectOwnership"/> from nested entity resolvers.
/// </summary>
[Tool]
[GlobalClass]
public partial class OwnershipResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "Ownership";

	/// <summary>
	/// Gets or sets the resolver used for the ownership owner entity.
	/// </summary>
	[Export]
	public EntityResolverResourceBase? Owner { get; set; }

	/// <summary>
	/// Gets or sets the resolver used for the ownership source entity.
	/// </summary>
	[Export]
	public EntityResolverResourceBase? Source { get; set; }

	[Export]
	public bool OwnerFolded { get; set; }

	[Export]
	public bool SourceFolded { get; set; }

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		var propertyKey = new StringKey($"__ownership_{nodeId}_{index}");
		graph.VariableDefinitions.DefineObjectProperty(propertyKey, BuildOwnershipResolver(graph));
		runtimeNode.BindInput(index, propertyKey);
	}

	/// <summary>
	/// Builds the core <see cref="OwnershipResolver"/> from the nested owner and source entity resolvers.
	/// </summary>
	/// <param name="graph">The runtime graph being built.</param>
	/// <returns>The composed ownership resolver.</returns>
	public OwnershipResolver BuildOwnershipResolver(Graph graph)
	{
		return new OwnershipResolver(
			Owner?.BuildEntityResolver(graph),
			Source?.BuildEntityResolver(graph));
	}
}
