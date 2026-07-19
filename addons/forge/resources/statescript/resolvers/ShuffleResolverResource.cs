// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that produces a random permutation of a nested array source. Combine with a Take resolver to
/// pick N random elements without repetition.
/// </summary>
[Tool]
[GlobalClass]
public partial class ShuffleResolverResource : ArrayTransformResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "Shuffle";

	/// <inheritdoc/>
	public override string PropertyNamePrefix => "shuffle";

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

		var random = new ForgeRandom();

		if (sourceObjectArray is not null)
		{
			objectArrayResolver = (IObjectArrayResolver)Activator.CreateInstance(
				typeof(ObjectShuffleResolver<>).MakeGenericType(sourceObjectArray.ElementType),
				sourceObjectArray,
				random)!;
			return true;
		}

		valueArrayResolver = new ShuffleResolver(sourceValueArray!, random);
		return true;
	}
}
