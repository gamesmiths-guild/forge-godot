// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that de-duplicates a nested array source, keeping the first occurrence of each element.
/// </summary>
[Tool]
[GlobalClass]
public partial class DistinctResolverResource : ArrayTransformResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "Distinct";

	/// <inheritdoc/>
	public override string PropertyNamePrefix => "distinct";

	/// <inheritdoc/>
	public override bool TryBuildArrayResolver(
		Graph graph,
		out IArrayPropertyResolver? valueArrayResolver,
		out IObjectArrayResolver? objectArrayResolver)
	{
		valueArrayResolver = null;
		objectArrayResolver = null;

		if (!TryResolveSource(
			graph,
			out IArrayPropertyResolver? sourceValueArray,
			out IObjectArrayResolver? sourceObjectArray))
		{
			return false;
		}

		if (sourceObjectArray is not null)
		{
			objectArrayResolver = ArrayResolverResourceUtilities.CreateObjectArrayOperation(
				typeof(ObjectDistinctResolver<>),
				sourceObjectArray);
			return true;
		}

		valueArrayResolver = new DistinctResolver(sourceValueArray!);
		return true;
	}
}
