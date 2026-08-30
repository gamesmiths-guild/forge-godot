// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads one of the facing directions of the 3D node an entity lives on.
/// </summary>
[Tool]
[GlobalClass]
public partial class EntityDirection3DResolverResource : SpatialResolverResourceBase3D
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "EntityDirection3D";

	/// <summary>
	/// Gets or sets which of the node's own directions to report.
	/// </summary>
	[Export]
	public SpatialAxis Axis { get; set; }

	/// <inheritdoc/>
	protected override string PropertyNamePrefix => "entitydirection3d";

	/// <inheritdoc/>
	protected override IPropertyResolver CreateResolver(IEntityResolver entityResolver, Graph graph)
	{
		return new EntityDirection3DResolver(entityResolver, NodePath, Axis);
	}
}
