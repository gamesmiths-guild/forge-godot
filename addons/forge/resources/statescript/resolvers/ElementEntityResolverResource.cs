// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads the entity array element currently being iterated by an enclosing array operation.
/// Use it inside nested predicate or key selector resolvers so entity-aware resolvers (attribute, tag query) read the
/// iterated entity.
/// </summary>
[Tool]
[GlobalClass]
public partial class ElementEntityResolverResource : EntityResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "ElementEntity";

	/// <inheritdoc/>
	public override IEntityResolver BuildEntityResolver(Graph graph)
	{
		return new ElementEntityResolver();
	}
}
