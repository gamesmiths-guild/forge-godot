// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that builds a rectangle for a query to sweep.
/// </summary>
[Tool]
[GlobalClass]
public partial class RectShape2DResolverResource : ShapeResolverResourceBase2D
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "RectShape2D";

	/// <summary>
	/// Gets or sets the nested resolver providing the full size.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Size { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the size section is folded in the editor.
	/// </summary>
	[Export]
	public bool SizeFolded { get; set; }

	/// <inheritdoc/>
	protected override string PropertyNamePrefix => "rectshape2d";

	/// <inheritdoc/>
	protected override IObjectResolver<Shape2D> CreateResolver(Graph graph)
	{
		return new RectShape2DResolver(BuildVectorDimension(Size, graph));
	}
}
