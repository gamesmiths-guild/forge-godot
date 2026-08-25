// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves the entity a scene node belongs to.
/// </summary>
/// <remarks>
/// <para>The crossing from the node lane back to the entity lane, and the counterpart of the spatial getters that go
/// the other way. A graph that instantiated a scene holds its root in a Node variable; a ray reports the collider it
/// met. Neither is an entity until this says which entity it belongs to, and until then nothing can be applied to it.
/// </para>
/// <para>The search is the same one physics hits use: the node itself, then its direct children, then up the ancestor
/// chain repeating that at each level. A collider is usually a hurtbox nested well below the node that owns the entity,
/// and a scene's root is usually above it, so looking in only one direction would miss half the cases.</para>
/// </remarks>
/// <param name="nodeResolver">Resolves the node to start the search from.</param>
internal sealed class EntityFromNodeResolver(IObjectResolver<Node> nodeResolver)
	: ObjectResolver<IForgeEntity>, IEntityResolver
{
	private readonly IObjectResolver<Node> _nodeResolver = nodeResolver;

	public override IForgeEntity? Resolve(GraphContext graphContext)
	{
		Node? node = _nodeResolver.Resolve(graphContext);

		return ForgeEntityBridge.TryGetEntityInHierarchy(node, out IForgeEntity? entity) ? entity : null;
	}
}
