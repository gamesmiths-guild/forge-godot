// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads the scale of the 2D node an entity lives on.
/// </summary>
[Tool]
[GlobalClass]
public partial class EntityScale2DResolverResource : SpatialResolverResourceBase2D
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "EntityScale2D";

	/// <summary>
	/// Gets or sets whether to read world or parent-relative scale.
	/// </summary>
	[Export]
	public TransformSpace Space { get; set; }

	/// <inheritdoc/>
	protected override string PropertyNamePrefix => "entityscale2d";

	/// <inheritdoc/>
	protected override IPropertyResolver CreateResolver(IEntityResolver entityResolver, Graph graph)
	{
		return new EntityScale2DResolver(entityResolver, NodePath, Space);
	}
}
