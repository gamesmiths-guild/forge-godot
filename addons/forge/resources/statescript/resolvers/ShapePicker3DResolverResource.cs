// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that authors a constant <see cref="Shape3D"/> reference.
/// </summary>
[Tool]
[GlobalClass]
public partial class ShapePicker3DResolverResource : ShapeResolverResourceBase3D
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "ShapePicker3D";

	/// <summary>
	/// Gets or sets the authored shape.
	/// </summary>
	[Export]
	public Shape3D? Shape { get; set; }

	/// <inheritdoc/>
	protected override string PropertyNamePrefix => "shapepicker3d";

	/// <inheritdoc/>
	protected override IObjectResolver<Shape3D> CreateResolver(Graph graph)
	{
		return new ShapePicker3DResolver(Shape);
	}
}
