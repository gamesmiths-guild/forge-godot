// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that builds a cone for a query to sweep, as a convex hull.
/// </summary>
[Tool]
[GlobalClass]
public partial class ConeShape3DResolverResource : ShapeResolverResourceBase3D
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "ConeShape3D";

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
	/// Gets or sets the nested resolver providing how far the cone reaches along its slant.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Range { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the range section is folded in the editor.
	/// </summary>
	[Export]
	public bool RangeFolded { get; set; }

	/// <inheritdoc/>
	protected override string PropertyNamePrefix => "coneshape3d";

	/// <inheritdoc/>
	protected override IObjectResolver<Shape3D> CreateResolver(Graph graph)
	{
		return new ConeShape3DResolver(BuildDimension(Angle, graph), BuildDimension(Range, graph));
	}
}
