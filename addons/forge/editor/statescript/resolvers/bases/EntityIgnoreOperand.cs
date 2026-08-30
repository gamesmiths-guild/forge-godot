// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;

/// <summary>
/// The starting value of an "entities this query passes through" operand.
/// </summary>
/// <remarks>
/// Every physics query a graph runs is run by somebody, standing somewhere, and that somebody is almost never meant to
/// be its own result: a blast centred on the caster should not hit the caster, and a ray fired from the caster's own
/// position starts inside the caster's own collider. Shared so every query starts the same way, since an author moving
/// between a cast and an overlap should not find two different defaults.
/// </remarks>
internal static class EntityIgnoreOperand
{
	/// <summary>
	/// Builds a fresh array of the ability's owner, for a query the owner runs on everyone else.
	/// </summary>
	/// <returns>The seed resource. A new one each call, because it is bound as-is.</returns>
	public static ArrayResolverResource BuildOwner()
	{
		return Build(new AbilityOwnerResolverResource());
	}

	/// <summary>
	/// Builds a fresh array of the ability's owner and its target, for a query whose two ends both sit inside a body.
	/// </summary>
	/// <returns>The seed resource. A new one each call, because it is bound as-is.</returns>
	public static ArrayResolverResource BuildOwnerAndTarget()
	{
		return Build(new AbilityOwnerResolverResource(), new AbilityTargetResolverResource());
	}

	private static ArrayResolverResource Build(params StatescriptResolverResource[] elements)
	{
		return new ArrayResolverResource
		{
			ObjectElementTypeId = "Entity",
			ElementResolvers = [.. elements],
		};
	}
}
#endif
