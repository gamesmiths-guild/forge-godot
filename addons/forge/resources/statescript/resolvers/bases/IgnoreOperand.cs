// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;

/// <summary>
/// Builds the "entities this query passes through" operand every physics query takes.
/// </summary>
/// <remarks>
/// Read through the untyped array interface rather than a typed one, so any source of entities works: an authored
/// Array, a variable holding one, or an overlap query's results fed straight in.
/// </remarks>
internal static class IgnoreOperand
{
	/// <summary>
	/// Builds the runtime resolver for an authored ignore operand.
	/// </summary>
	/// <param name="resource">The authored operand, when there is one.</param>
	/// <param name="graph">The runtime graph being built.</param>
	/// <returns>The array resolver, or <see langword="null"/> when nothing is ignored.</returns>
	public static IObjectArrayResolver? Build(StatescriptResolverResource? resource, Graph graph)
	{
		return resource is not null
			&& resource.TryBuildArrayResolver(graph, out _, out IObjectArrayResolver? objectArrayResolver)
				? objectArrayResolver
				: null;
	}
}
