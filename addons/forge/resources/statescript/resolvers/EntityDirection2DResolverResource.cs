// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads one of the facing directions of the 2D node an entity lives on.
/// </summary>
[Tool]
[GlobalClass]
public partial class EntityDirection2DResolverResource : SpatialResolverResourceBase2D
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "EntityDirection2D";

	/// <summary>
	/// Gets or sets which of the node's own directions to report.
	/// </summary>
	[Export]
	public SpatialAxis2D Axis { get; set; }

	/// <inheritdoc/>
	protected override string PropertyNamePrefix => "entitydirection2d";

	/// <inheritdoc/>
	protected override IPropertyResolver CreateResolver(IEntityResolver entityResolver, Graph graph)
	{
		return new EntityDirection2DResolver(entityResolver, NodePath, Axis);
	}
}
