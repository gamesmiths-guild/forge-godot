// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads how fast the body an entity lives on is spinning.
/// </summary>
[Tool]
[GlobalClass]
public partial class EntityAngularVelocity2DResolverResource : SpatialResolverResourceBase2D
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "EntityAngularVelocity2D";

	/// <inheritdoc/>
	protected override string PropertyNamePrefix => "entityangularvelocity2d";

	/// <inheritdoc/>
	protected override IPropertyResolver CreateResolver(IEntityResolver entityResolver, Graph graph)
	{
		return new EntityAngularVelocity2DResolver(entityResolver, NodePath);
	}
}
