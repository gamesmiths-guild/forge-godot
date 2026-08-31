// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using NumericsQuaternion = System.Numerics.Quaternion;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads the first entity a shape swept through the world meets.
/// </summary>
[Tool]
[GlobalClass]
public partial class Shapecast3DResolverResource : EntityResolverResourceBase
{
	/// <summary>
	/// How far a sweep reaches when nothing was authored. A nested operand has no unbound state, and a sweep of zero
	/// length never leaves where it started.
	/// </summary>
	private const double DefaultMaxDistance = 10.0;

	/// <inheritdoc/>
	public override string ResolverTypeId => "Shapecast3D";

	/// <summary>
	/// Gets or sets the nested resolver providing the shape to sweep.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Shape { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the shape section is folded in the editor.
	/// </summary>
	[Export]
	public bool ShapeFolded { get; set; }

	/// <summary>
	/// Gets or sets the nested resolver providing where the sweep starts.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Origin { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the origin section is folded in the editor.
	/// </summary>
	[Export]
	public bool OriginFolded { get; set; } = true;

	/// <summary>
	/// Gets or sets the nested resolver providing which way the sweep goes.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Direction { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the direction section is folded in the editor.
	/// </summary>
	[Export]
	public bool DirectionFolded { get; set; } = true;

	/// <summary>
	/// Gets or sets the nested resolver providing how far the sweep reaches.
	/// </summary>
	[Export]
	public StatescriptResolverResource? MaxDistance { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the max distance section is folded in the editor.
	/// </summary>
	[Export]
	public bool MaxDistanceFolded { get; set; }

	/// <summary>
	/// Gets or sets the nested resolver providing how the shape is turned. Unset leaves it upright.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Rotation { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the rotation section is folded in the editor.
	/// </summary>
	[Export]
	public bool RotationFolded { get; set; } = true;

	/// <summary>
	/// Gets or sets the nested resolver providing the physics layers the sweep can hit. Zero means every layer.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Mask { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the mask section is folded in the editor.
	/// </summary>
	[Export]
	public bool MaskFolded { get; set; } = true;

	/// <summary>
	/// Gets or sets a value indicating whether areas stop the sweep, as well as bodies.
	/// </summary>
	[Export]
	public bool CollideWithAreas { get; set; }

	/// <summary>
	/// Gets or sets the entities the sweep passes through, normally the caster. Unset passes through nothing.
	/// </summary>
	[Export]
	public StatescriptResolverResource? IgnoreResolver { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the ignore section is folded in the editor.
	/// </summary>
	[Export]
	public bool IgnoreFolded { get; set; } = true;

	/// <inheritdoc/>
	public override IEntityResolver BuildEntityResolver(Graph graph)
	{
		IPropertyResolver originResolver = Origin is null
			? new EntityPosition3DResolver(new AbilityOwnerResolver(), string.Empty, TransformSpace.Global)
			: AdaptResolverForExpectedType(Origin.BuildResolver(graph), typeof(NumericsVector3));

		IPropertyResolver directionResolver = Direction is null
			? new EntityDirection3DResolver(new AbilityOwnerResolver(), string.Empty, SpatialAxis.Forward)
			: AdaptResolverForExpectedType(Direction.BuildResolver(graph), typeof(NumericsVector3));

		IPropertyResolver maxDistanceResolver = MaxDistance is null
			? new VariantResolver(new Variant128(DefaultMaxDistance), typeof(double))
			: AdaptResolverForExpectedType(MaxDistance.BuildResolver(graph), typeof(double));

		IPropertyResolver? rotationResolver = Rotation is null
			? null
			: AdaptResolverForExpectedType(Rotation.BuildResolver(graph), typeof(NumericsQuaternion));

		return new Shapecast3DResolver(
			BuildShapeResolver(graph),
			originResolver,
			directionResolver,
			maxDistanceResolver,
			rotationResolver,
			Mask?.BuildResolver(graph),
			CollideWithAreas,
			IgnoreOperand.Build(IgnoreResolver, graph));
	}

	private IObjectResolver<Shape3D> BuildShapeResolver(Graph graph)
	{
		// An unset shape resolves to nothing rather than to a default sphere, matching Overlap 3D: a sweep with no
		// shape authored should find nothing and be obvious about it.
		if (Shape is null
			|| !Shape.TryBuildObjectResolver(graph, out IObjectResolver? objectResolver)
			|| objectResolver is not IObjectResolver<Shape3D> shapeResolver)
		{
			return new ShapePicker3DResolver(null);
		}

		return shapeResolver;
	}
}
