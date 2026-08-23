// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads the position of the 3D node an entity lives on.
/// </summary>
[Tool]
[GlobalClass]
public partial class Position3DResolverResource : SpatialResolverResourceBase3D
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "Position3D";

	/// <summary>
	/// Gets or sets whether to read world or parent-relative position.
	/// </summary>
	[Export]
	public TransformSpace Space { get; set; }

	/// <inheritdoc/>
	protected override string PropertyNamePrefix => "position3d";

	/// <inheritdoc/>
	protected override IPropertyResolver CreateResolver(IEntityResolver entityResolver, Graph graph)
	{
		return new Position3DResolver(entityResolver, NodePath, Space);
	}
}
