// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves the entities nested inside an entity.
/// </summary>
/// <remarks>
/// <para>The other direction of Parent Entity, and the one that acts on a group: a builder revoking every turret it
/// placed, a summoner buffing everything it summoned, a vehicle applying a hit to whoever is sitting in it. The
/// entities are already in the scene tree under the one being read, so nothing had to be written to a variable when
/// they were made.</para>
/// <para>The walk stops at each entity it finds rather than descending through it. What is inside a turret belongs to
/// the turret, and a builder asking for its own children should not be handed its turrets' passengers as if they were
/// its own - one more Child Entities on the turret is how those are reached, which is also what makes the two levels
/// tell themselves apart.</para>
/// <para>Levels that resolve back to the entity being read are stepped over rather than reported, for the same reason
/// Parent Entity steps over them: under the composition pattern an entity's own node sits below its body, so a naive
/// walk would report every entity as containing itself.</para>
/// </remarks>
/// <param name="entityResolver">Resolves which entity to read.</param>
internal sealed class ChildEntitiesResolver(IEntityResolver entityResolver) : ObjectArrayResolver<IForgeEntity>
{
	private readonly IEntityResolver _entityResolver = entityResolver;
	private readonly List<IForgeEntity> _found = [];

	public override IForgeEntity[] ResolveArray(GraphContext graphContext)
	{
		IForgeEntity? entity = _entityResolver.Resolve(graphContext);

		if (entity is null || !ForgeEntityBridge.TryGetOwningNode(entity, string.Empty, out Node? root))
		{
			return [];
		}

		// The list is kept between resolves so a per-tick read does not allocate one each time. Forge runs its graphs
		// on one thread, and the contents never outlive the copy returned below.
		_found.Clear();
		Collect(root, entity, _found);

		return [.. _found];
	}

	private static void Collect(Node node, IForgeEntity self, List<IForgeEntity> into)
	{
		foreach (Node child in node.GetChildren())
		{
			if (ForgeEntityBridge.TryGetEntity(child, out IForgeEntity? candidate)
				&& !ReferenceEquals(candidate, self))
			{
				into.Add(candidate);
				continue;
			}

			Collect(child, self, into);
		}
	}
}
