// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;

/// <summary>
/// Shared helpers for ability data resolver resources.
/// </summary>
internal static class AbilityResolverResourceUtilities
{
	/// <summary>
	/// Builds the optional ability handle source of an ability data resolver.
	/// </summary>
	/// <param name="ability">The nested resolver resource providing the handle, or <see langword="null"/> to read
	/// the ability driving the graph.</param>
	/// <param name="graph">The runtime graph being built.</param>
	/// <returns>The typed handle resolver, or <see langword="null"/> when unset or incompatible.</returns>
	public static IObjectResolver<AbilityHandle>? BuildHandleResolver(
		StatescriptResolverResource? ability,
		Graph graph)
	{
		if (ability is not null
			&& ability.TryBuildObjectResolver(graph, out IObjectResolver? handleResolver)
			&& handleResolver is IObjectResolver<AbilityHandle> typedHandleResolver)
		{
			return typedHandleResolver;
		}

		return null;
	}
}
