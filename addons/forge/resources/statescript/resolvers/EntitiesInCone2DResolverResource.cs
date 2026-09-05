// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using ForgeNode = Gamesmiths.Forge.Statescript.Node;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads the entities standing inside a wedge opening from a point.
/// </summary>
[Tool]
[GlobalClass]
public partial class EntitiesInCone2DResolverResource : StatescriptResolverResource
{
	/// <summary>
	/// How far a wedge reaches when nothing was authored. A nested operand has no unbound state, and a range of zero
	/// finds nobody, so a fresh wedge needs a reach that shows something. Larger than the 3D default because a 2D
	/// world measures distance in pixels.
	/// </summary>
	private const double DefaultRange = 200.0;

	/// <summary>
	/// How wide a wedge opens when nothing was authored, in degrees. A quarter turn is the cleave everyone draws
	/// first.
	/// </summary>
	private const double DefaultAngle = 90.0;

	/// <inheritdoc/>
	public override string ResolverTypeId => "EntitiesInCone2D";

	/// <summary>
	/// Gets or sets the nested resolver providing the wedge's apex.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Origin { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the origin section is folded in the editor.
	/// </summary>
	[Export]
	public bool OriginFolded { get; set; } = true;

	/// <summary>
	/// Gets or sets the nested resolver providing which way the wedge opens.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Direction { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the direction section is folded in the editor.
	/// </summary>
	[Export]
	public bool DirectionFolded { get; set; } = true;

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
	/// Gets or sets the nested resolver providing the physics layers the query can find. Zero means every layer.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Mask { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the mask section is folded in the editor.
	/// </summary>
	[Export]
	public bool MaskFolded { get; set; } = true;

	/// <summary>
	/// Gets or sets a value indicating whether areas count as overlaps, as well as bodies.
	/// </summary>
	[Export]
	public bool IncludeAreas { get; set; }

	/// <summary>
	/// Gets or sets the entities left out of the results, normally the caster. Unset leaves out nothing.
	/// </summary>
	[Export]
	public StatescriptResolverResource? IgnoreResolver { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the ignore section is folded in the editor.
	/// </summary>
	[Export]
	public bool IgnoreFolded { get; set; } = true;

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		TryBuildArrayResolver(graph, out _, out IObjectArrayResolver? objectArrayResolver);

		var propertyName = new StringKey($"__entitiesincone2d_{nodeId}_{index}");
		graph.VariableDefinitions.DefineObjectArrayProperty(propertyName, objectArrayResolver!);
		runtimeNode.BindInput(index, propertyName);
	}

	/// <inheritdoc/>
	public override bool TryBuildArrayResolver(
		Graph graph,
		out IArrayPropertyResolver? valueArrayResolver,
		out IObjectArrayResolver? objectArrayResolver)
	{
		valueArrayResolver = null;

		// The seeded operands fall back to what the editor seeds them with, so a resource built outside the editor
		// runs the same query a fresh one does rather than a wedge of no reach centred on the world origin.
		IPropertyResolver originResolver = Origin is null
			? new EntityPosition2DResolver(new AbilityOwnerResolver(), string.Empty, TransformSpace.Global)
			: AdaptResolverForExpectedType(Origin.BuildResolver(graph), typeof(NumericsVector2));

		IPropertyResolver directionResolver = Direction is null
			? new EntityDirection2DResolver(new AbilityOwnerResolver(), string.Empty, SpatialAxis2D.Forward)
			: AdaptResolverForExpectedType(Direction.BuildResolver(graph), typeof(NumericsVector2));

		objectArrayResolver = new EntitiesInCone2DResolver(
			originResolver,
			directionResolver,
			BuildNumberOrDefault(Range, graph, DefaultRange),
			BuildNumberOrDefault(Angle, graph, DefaultAngle),
			Mask?.BuildResolver(graph),
			IncludeAreas,
			IgnoreOperand.Build(IgnoreResolver, graph));

		return true;
	}

	private static IPropertyResolver BuildNumberOrDefault(
		StatescriptResolverResource? resource,
		Graph graph,
		double fallback)
	{
		return resource is null
			? new VariantResolver(new Variant128(fallback), typeof(double))
			: AdaptResolverForExpectedType(resource.BuildResolver(graph), typeof(double));
	}
}
