// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that authors a constant scene-node reference as a path.
/// </summary>
/// <remarks>
/// Node settings only carry primitives and a node cannot be exported on a resource, so an authored path is how a graph
/// names a node that already exists in the level - a container to parent spawns under, a marker, a prop - rather than
/// one it made itself and stored in a variable.
/// </remarks>
[Tool]
[GlobalClass]
public partial class NodePathResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "NodePath";

	/// <summary>
	/// Gets or sets the path to resolve, from the current scene's root.
	/// </summary>
	/// <remarks>
	/// Absolute paths (<c>/root/Main/Props</c>) and scene-unique names (<c>%SpawnPoint</c>) work, the latter for nodes
	/// marked unique in the current scene itself.
	/// </remarks>
	[Export]
	public string NodePath { get; set; } = string.Empty;

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		if (NodePath.Length == 0)
		{
			return;
		}

		var propertyName = new StringKey($"__node_path_{nodeId}_{index}");
		graph.VariableDefinitions.DefineObjectProperty(propertyName, new NodePathResolver(NodePath));
		runtimeNode.BindInput(index, propertyName);
	}

	/// <inheritdoc/>
	public override bool TryBuildObjectResolver(Graph graph, out IObjectResolver? objectResolver)
	{
		objectResolver = null;

		if (NodePath.Length == 0)
		{
			return false;
		}

		objectResolver = new NodePathResolver(NodePath);
		return true;
	}
}
