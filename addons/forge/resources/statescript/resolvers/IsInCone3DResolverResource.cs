// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reports whether a point falls inside a cone.
/// </summary>
[Tool]
[GlobalClass]
public partial class IsInCone3DResolverResource : ValueResolverResourceBase
{
	/// <summary>
	/// How wide a cone opens when nothing was authored, in degrees. A quarter turn is the arc everyone draws first.
	/// </summary>
	private const double DefaultAngle = 90.0;

	/// <inheritdoc/>
	public override string ResolverTypeId => "IsInCone3D";

	/// <summary>
	/// Gets or sets the nested resolver providing the point being tested.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Point { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the point section is folded in the editor.
	/// </summary>
	[Export]
	public bool PointFolded { get; set; }

	/// <summary>
	/// Gets or sets the nested resolver providing the cone's apex.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Origin { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the origin section is folded in the editor.
	/// </summary>
	[Export]
	public bool OriginFolded { get; set; } = true;

	/// <summary>
	/// Gets or sets the nested resolver providing which way the cone opens.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Direction { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the direction section is folded in the editor.
	/// </summary>
	[Export]
	public bool DirectionFolded { get; set; } = true;

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
	/// Gets or sets the nested resolver providing how far the cone reaches. Unset, or zero, means no limit.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Range { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the range section is folded in the editor.
	/// </summary>
	[Export]
	public bool RangeFolded { get; set; } = true;

	/// <inheritdoc/>
	protected override string PropertyNamePrefix => "isincone3d";

	/// <inheritdoc/>
	protected override IPropertyResolver CreateResolver(Graph graph)
	{
		// The point, apex and facing all fall back to what the editor seeds them with, so a resource built outside the
		// editor asks the same question a fresh one does rather than testing the world origin against nothing.
		IPropertyResolver pointResolver = Point is null
			? new EntityPosition3DResolver(new AbilityTargetResolver(), string.Empty, TransformSpace.Global)
			: AdaptResolverForExpectedType(Point.BuildResolver(graph), typeof(NumericsVector3));

		IPropertyResolver originResolver = Origin is null
			? new EntityPosition3DResolver(new AbilityOwnerResolver(), string.Empty, TransformSpace.Global)
			: AdaptResolverForExpectedType(Origin.BuildResolver(graph), typeof(NumericsVector3));

		IPropertyResolver directionResolver = Direction is null
			? new EntityDirection3DResolver(new AbilityOwnerResolver(), string.Empty, SpatialAxis.Forward)
			: AdaptResolverForExpectedType(Direction.BuildResolver(graph), typeof(NumericsVector3));

		IPropertyResolver angleResolver = Angle is null
			? new VariantResolver(new Variant128(DefaultAngle), typeof(double))
			: AdaptResolverForExpectedType(Angle.BuildResolver(graph), typeof(double));

		return new IsInCone3DResolver(
			pointResolver,
			originResolver,
			directionResolver,
			angleResolver,
			Range is null ? null : AdaptResolverForExpectedType(Range.BuildResolver(graph), typeof(double)));
	}
}
