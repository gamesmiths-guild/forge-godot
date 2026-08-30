// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads the scale of the 3D node an entity lives on.
/// </summary>
[Tool]
[GlobalClass]
public partial class EntityScale3DResolverResource : SpatialResolverResourceBase3D
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "EntityScale3D";

	/// <summary>
	/// Gets or sets whether to read world or parent-relative scale.
	/// </summary>
	[Export]
	public TransformSpace Space { get; set; }

	/// <inheritdoc/>
	protected override string PropertyNamePrefix => "entityscale3d";

	/// <inheritdoc/>
	protected override IPropertyResolver CreateResolver(IEntityResolver entityResolver, Graph graph)
	{
		return new EntityScale3DResolver(entityResolver, NodePath, Space);
	}
}
