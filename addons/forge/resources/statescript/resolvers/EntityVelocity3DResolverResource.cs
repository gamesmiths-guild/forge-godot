// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads the linear velocity of the body an entity lives on.
/// </summary>
[Tool]
[GlobalClass]
public partial class EntityVelocity3DResolverResource : SpatialResolverResourceBase3D
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "EntityVelocity3D";

	/// <inheritdoc/>
	protected override string PropertyNamePrefix => "entityvelocity3d";

	/// <inheritdoc/>
	protected override IPropertyResolver CreateResolver(IEntityResolver entityResolver, Graph graph)
	{
		return new EntityVelocity3DResolver(entityResolver, NodePath);
	}
}
