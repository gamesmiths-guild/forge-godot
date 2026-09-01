// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that authors a constant <see cref="Shape2D"/> reference.
/// </summary>
[Tool]
[GlobalClass]
public partial class ShapePicker2DResolverResource : ShapeResolverResourceBase2D
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "ShapePicker2D";

	/// <summary>
	/// Gets or sets the authored shape.
	/// </summary>
	[Export]
	public Shape2D? Shape { get; set; }

	/// <inheritdoc/>
	protected override string PropertyNamePrefix => "shapepicker2d";

	/// <inheritdoc/>
	protected override IObjectResolver<Shape2D> CreateResolver(Graph graph)
	{
		return new ShapePicker2DResolver(Shape);
	}
}
