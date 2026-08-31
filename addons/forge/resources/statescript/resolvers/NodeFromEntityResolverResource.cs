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
/// Resolver resource that reads the scene node an entity lives on.
/// </summary>
[Tool]
[GlobalClass]
public partial class NodeFromEntityResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "NodeFromEntity";

	/// <summary>
	/// Gets or sets which entity to read. Defaults to the ability's owner when left unset.
	/// </summary>
	[Export]
	public StatescriptResolverResource? EntityResolver { get; set; }

	/// <summary>
	/// Gets or sets an optional path to a descendant node to return instead of the entity's own node.
	/// </summary>
	/// <remarks>
	/// Scene-unique names (<c>%Muzzle</c>) work, resolved relative to the entity rather than to the scene.
	/// </remarks>
	[Export]
	public string NodePath { get; set; } = string.Empty;

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		var propertyName = new StringKey($"__node_from_entity_{nodeId}_{index}");
		graph.VariableDefinitions.DefineObjectProperty(propertyName, BuildResolverInstance(graph));
		runtimeNode.BindInput(index, propertyName);
	}

	/// <inheritdoc/>
	public override bool TryBuildObjectResolver(Graph graph, out IObjectResolver? objectResolver)
	{
		objectResolver = BuildResolverInstance(graph);
		return true;
	}

	private NodeFromEntityResolver BuildResolverInstance(Graph graph)
	{
		return new NodeFromEntityResolver(EntityOperand.BuildOrOwner(EntityResolver, graph), NodePath);
	}
}
