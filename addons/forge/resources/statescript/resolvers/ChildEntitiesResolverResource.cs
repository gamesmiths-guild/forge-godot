// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads the entities nested inside an entity.
/// </summary>
[Tool]
[GlobalClass]
public partial class ChildEntitiesResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "ChildEntities";

	/// <summary>
	/// Gets or sets which entity to read. Defaults to the ability's owner when left unset.
	/// </summary>
	[Export]
	public StatescriptResolverResource? EntityResolver { get; set; }

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		TryBuildArrayResolver(graph, out _, out IObjectArrayResolver? objectArrayResolver);

		var propertyName = new StringKey($"__childentities_{nodeId}_{index}");
		graph.VariableDefinitions.DefineObjectArrayProperty(propertyName, objectArrayResolver!);
		runtimeNode.BindInput(index, propertyName);
	}

	/// <inheritdoc/>
	public override bool TryBuildArrayResolver(
		Graph graph,
		out IArrayPropertyResolver? valueArrayResolver,
		out IObjectArrayResolver? objectArrayResolver)
	{
		valueArrayResolver = null;
		objectArrayResolver = new ChildEntitiesResolver(EntityOperand.BuildOrOwner(EntityResolver, graph));

		return true;
	}
}
