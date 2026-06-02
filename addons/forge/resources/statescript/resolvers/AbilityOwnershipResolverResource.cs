// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads the current ability ownership from the active ability context.
/// </summary>
[Tool]
[GlobalClass]
public partial class AbilityOwnershipResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "AbilityOwnership";

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		var propertyKey = new StringKey($"__ability_ownership_{nodeId}_{index}");
		graph.VariableDefinitions.DefineObjectProperty(propertyKey, new AbilityOwnershipResolver());
		runtimeNode.BindInput(index, propertyKey);
	}
}
