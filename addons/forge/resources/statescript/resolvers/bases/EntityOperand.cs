// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;

/// <summary>
/// Builds the "which entity" operand that most resolvers take.
/// </summary>
/// <remarks>
/// The operand is an ordinary nested resolver rather than a closed list of the four or five entities an ability knows
/// about. That is what lets a graph read a position off the first thing an overlap found, or off an entity named by a
/// scene path, without every resolver that takes an entity having to grow a new dropdown entry for each of them.
/// </remarks>
internal static class EntityOperand
{
	/// <summary>
	/// Builds the runtime resolver for an authored entity operand.
	/// </summary>
	/// <param name="resource">The authored operand, when there is one.</param>
	/// <param name="graph">The runtime graph being built.</param>
	/// <returns>The entity resolver, or <see langword="null"/> when nothing was authored.</returns>
	public static IEntityResolver? Build(StatescriptResolverResource? resource, Graph graph)
	{
		if (resource is null
			|| !resource.TryBuildObjectResolver(graph, out IObjectResolver? objectResolver)
			|| objectResolver is null)
		{
			return null;
		}

		return objectResolver as IEntityResolver ?? new EntityObjectResolver(objectResolver);
	}

	/// <summary>
	/// Builds the runtime resolver for an authored entity operand, falling back to the ability's owner.
	/// </summary>
	/// <param name="resource">The authored operand, when there is one.</param>
	/// <param name="graph">The runtime graph being built.</param>
	/// <returns>The entity resolver.</returns>
	public static IEntityResolver BuildOrOwner(StatescriptResolverResource? resource, Graph graph)
	{
		return Build(resource, graph) ?? new AbilityOwnerResolver();
	}
}
