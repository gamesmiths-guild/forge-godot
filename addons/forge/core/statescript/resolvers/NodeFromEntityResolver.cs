// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves the scene node an entity lives on, or a named child of it.
/// </summary>
/// <remarks>
/// <para>The crossing from the entity lane into the node lane, and the counterpart of Entity From Node. It is what
/// hands a node to everything that takes one - a property write, a method call, a reparent - without the graph having
/// to know where in the level that entity happens to be, and it is how an entity reaches a Godot property that expects
/// a node.</para>
/// <para>Dimension-neutral: the entity's nearest spatial ancestor of either kind, which is the node a 2D and a 3D game
/// both mean by "the entity's node". An authored path names a child of it instead, and scene-unique names
/// (<c>%Muzzle</c>) work, resolved relative to the entity rather than to the scene.</para>
/// <para>A path that finds nothing resolves to null rather than falling back to the entity's own node. The path names
/// <em>which</em> node is wanted, so substituting a different one would answer a question nobody asked - unlike a
/// spatial getter's marker, which is an offset from a subject that is already decided.</para>
/// </remarks>
/// <param name="entityResolver">Resolves which entity to read.</param>
/// <param name="nodePath">Optional path to a descendant node to return instead of the entity's own node.</param>
internal sealed class NodeFromEntityResolver(IEntityResolver entityResolver, string nodePath) : ObjectResolver<Node>
{
	private readonly IEntityResolver _entityResolver = entityResolver;
	private readonly string _nodePath = nodePath ?? string.Empty;

	private bool _reportedMissingNode;

	public override Node? Resolve(GraphContext graphContext)
	{
		IForgeEntity? entity = _entityResolver.Resolve(graphContext);

		if (ForgeEntityBridge.TryGetOwningNode(entity, _nodePath, out Node? node))
		{
			return node;
		}

		if (entity is null)
		{
			ReportMissingNodeOnce(
				"resolved no entity to read from. Check the entity operand, which is often an empty variable.");

			return null;
		}

		string at = _nodePath.Length == 0 ? string.Empty : $" at [{_nodePath}]";
		ReportMissingNodeOnce($"found no node for its entity{at}.");

		return null;
	}

	// Resolvers run every tick, so a warning left unsuppressed would repeat every frame.
	private void ReportMissingNodeOnce(string message)
	{
		if (_reportedMissingNode)
		{
			return;
		}

		_reportedMissingNode = true;

		GD.PushWarning($"Statescript: NodeFromEntity {message} Resolving to null.");
	}
}
