// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using GodotNode = Godot.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads the entity a scene node belongs to.
/// </summary>
[Tool]
[GlobalClass]
public partial class EntityFromNodeResolverResource : EntityResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "EntityFromNode";

	/// <summary>
	/// Gets or sets the nested resolver providing the node to search from.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Node { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the node section is folded in the editor.
	/// </summary>
	[Export]
	public bool NodeFolded { get; set; }

	/// <inheritdoc/>
	public override IEntityResolver BuildEntityResolver(Graph graph)
	{
		// An unset node resolves to nothing rather than to some fallback entity: a graph that has not said which node
		// to look at should find nobody, not quietly find the caster.
		if (Node is null
			|| !Node.TryBuildObjectResolver(graph, out IObjectResolver? objectResolver)
			|| objectResolver is not IObjectResolver<GodotNode> nodeResolver)
		{
			return new EntityFromNodeResolver(new NodePathResolver(string.Empty));
		}

		return new EntityFromNodeResolver(nodeResolver);
	}
}
