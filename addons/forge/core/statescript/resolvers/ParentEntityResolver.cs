// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves the entity an entity is nested inside.
/// </summary>
/// <remarks>
/// <para>Entities nest whenever a game spawns one from another and keeps them together: a turret placed by a builder,
/// a summon that dies with its summoner, a vehicle whose seats are entities of their own. This reads the one above,
/// which is how a graph running on the inner one reaches the outer one's attributes, tags and effects without a
/// variable being written for it.</para>
/// <para>The search starts strictly <em>above</em> the entity's own node and climbs one level at a time, checking each
/// ancestor and its direct children. Levels that resolve back to the same entity are stepped over rather than stopping
/// the walk: under the composition pattern an entity's node is a child of its body, so the body is a level whose
/// children include the entity itself, and treating that as the answer would make everything its own parent.</para>
/// <para>An entity with nothing above it resolves to null, which is the honest answer and not an error: most entities
/// in a level are top level, and the same graph runs on those.</para>
/// </remarks>
/// <param name="entityResolver">Resolves which entity to read.</param>
internal sealed class ParentEntityResolver(IEntityResolver entityResolver)
	: ObjectResolver<IForgeEntity>, IEntityResolver
{
	private readonly IEntityResolver _entityResolver = entityResolver;

	public override IForgeEntity? Resolve(GraphContext graphContext)
	{
		IForgeEntity? entity = _entityResolver.Resolve(graphContext);

		if (!ForgeEntityBridge.TryGetEntityNode(entity, out Node? entityNode))
		{
			return null;
		}

		Node? ancestor = entityNode.GetParent();

		while (ancestor is not null)
		{
			if (ForgeEntityBridge.TryGetEntity(ancestor, out IForgeEntity? candidate)
				&& !ReferenceEquals(candidate, entity))
			{
				return candidate;
			}

			ancestor = ancestor.GetParent();
		}

		return null;
	}
}
