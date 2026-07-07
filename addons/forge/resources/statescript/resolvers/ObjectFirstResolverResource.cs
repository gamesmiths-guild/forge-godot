// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads the first element of a nested object array source of any registered object type.
/// </summary>
[Tool]
[GlobalClass]
public partial class ObjectFirstResolverResource : ObjectArrayAccessResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "ObjectFirst";

	/// <inheritdoc/>
	public override string PropertyNamePrefix => "objectfirst";

	/// <inheritdoc/>
	public override IObjectResolver BuildObjectAccessResolver(Graph graph, IObjectArrayResolver source)
	{
		return ArrayResolverResourceUtilities.CreateObjectAccessResolver(typeof(ObjectFirstResolver<>), source);
	}
}
