// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that builds a cylinder for a query to sweep.
/// </summary>
[Tool]
[GlobalClass]
public partial class CylinderShape3DResolverResource : ShapeResolverResourceBase3D
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "CylinderShape3D";

	/// <summary>
	/// Gets or sets the nested resolver providing the radius.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Radius { get; set; }

	/// <summary>
	/// Gets or sets the nested resolver providing the total height.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Height { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the radius section is folded in the editor.
	/// </summary>
	[Export]
	public bool RadiusFolded { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the height section is folded in the editor.
	/// </summary>
	[Export]
	public bool HeightFolded { get; set; }

	/// <inheritdoc/>
	protected override string PropertyNamePrefix => "cylindershape3d";

	/// <inheritdoc/>
	protected override IObjectResolver<Shape3D> CreateResolver(Graph graph)
	{
		return new CylinderShape3DResolver(BuildDimension(Radius, graph), BuildDimension(Height, graph));
	}
}
