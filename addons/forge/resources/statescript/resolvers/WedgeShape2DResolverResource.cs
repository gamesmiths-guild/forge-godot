// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that builds a wedge for a query to sweep, as a convex polygon.
/// </summary>
[Tool]
[GlobalClass]
public partial class WedgeShape2DResolverResource : ShapeResolverResourceBase2D
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "WedgeShape2D";

	/// <summary>
	/// Gets or sets the nested resolver providing the full aperture, in degrees.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Angle { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the angle section is folded in the editor.
	/// </summary>
	[Export]
	public bool AngleFolded { get; set; }

	/// <summary>
	/// Gets or sets the nested resolver providing how far the wedge reaches.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Range { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the range section is folded in the editor.
	/// </summary>
	[Export]
	public bool RangeFolded { get; set; }

	/// <inheritdoc/>
	protected override string PropertyNamePrefix => "wedgeshape2d";

	/// <inheritdoc/>
	protected override IObjectResolver<Shape2D> CreateResolver(Graph graph)
	{
		return new WedgeShape2DResolver(BuildDimension(Angle, graph), BuildDimension(Range, graph));
	}
}
