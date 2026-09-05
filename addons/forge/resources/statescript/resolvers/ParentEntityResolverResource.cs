// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads the entity an entity is nested inside.
/// </summary>
[Tool]
[GlobalClass]
public partial class ParentEntityResolverResource : EntityResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "ParentEntity";

	/// <summary>
	/// Gets or sets which entity to read. Defaults to the ability's owner when left unset.
	/// </summary>
	[Export]
	public StatescriptResolverResource? EntityResolver { get; set; }

	/// <inheritdoc/>
	public override IEntityResolver BuildEntityResolver(Graph graph)
	{
		return new ParentEntityResolver(EntityOperand.BuildOrOwner(EntityResolver, graph));
	}
}
