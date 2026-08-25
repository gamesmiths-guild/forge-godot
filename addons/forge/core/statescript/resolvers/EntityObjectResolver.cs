// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Presents any object-lane resolver that yields entities as an <see cref="IEntityResolver"/>.
/// </summary>
/// <remarks>
/// Entity operands are authored as ordinary nested resolvers, so what comes back is whatever the author picked - an
/// ability owner, a variable, the first element of an overlap query, a random one of them. Only some of those declare
/// themselves as entity resolvers; the rest are object resolvers that happen to produce entities. This adapts the
/// second kind rather than shutting them out, which is what lets "the closest thing my blast found" be the entity a
/// position is read from.
/// </remarks>
/// <param name="inner">The resolver to adapt.</param>
internal sealed class EntityObjectResolver(IObjectResolver inner) : ObjectResolver<IForgeEntity>, IEntityResolver
{
	private readonly IObjectResolver _inner = inner;

	public override IForgeEntity? Resolve(GraphContext graphContext)
	{
		return _inner.Resolve(graphContext) as IForgeEntity;
	}
}
