// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using ForgeNode = Gamesmiths.Forge.Statescript.Node;
using NumericsQuaternion = System.Numerics.Quaternion;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads the entities inside a shape swept through the world at query time.
/// </summary>
[Tool]
[GlobalClass]
public partial class Overlap3DResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "Overlap3D";

	/// <summary>
	/// Gets or sets the nested resolver providing the shape to sweep.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Shape { get; set; }

	/// <summary>
	/// Gets or sets the nested resolver providing where the shape sits.
	/// </summary>
	/// <remarks>
	/// Unset only when the resource was built outside the editor, where it stands for the entity's own position. The
	/// editor always authors one, starting on an Entity Position 3D resolver so a fresh query is centred on the
	/// caster.
	/// </remarks>
	[Export]
	public StatescriptResolverResource? Position { get; set; }

	/// <summary>
	/// Gets or sets the nested resolver providing how the shape is turned. Unset leaves it upright.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Rotation { get; set; }

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

	/// <summary>
	/// Gets or sets a value indicating whether the shape section is folded in the editor.
	/// </summary>
	[Export]
	public bool ShapeFolded { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the position section is folded in the editor.
	/// </summary>
	[Export]
	public bool PositionFolded { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the rotation section is folded in the editor.
	/// </summary>
	[Export]
	public bool RotationFolded { get; set; } = true;

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		TryBuildArrayResolver(graph, out _, out IObjectArrayResolver? objectArrayResolver);

		var propertyName = new StringKey($"__overlap3d_{nodeId}_{index}");
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

		IPropertyResolver positionResolver = Position is null
			? new EntityPosition3DResolver(new AbilityOwnerResolver(), string.Empty, TransformSpace.Global)
			: AdaptResolverForExpectedType(Position.BuildResolver(graph), typeof(NumericsVector3));

		IPropertyResolver? rotationResolver = Rotation is null
			? null
			: AdaptResolverForExpectedType(Rotation.BuildResolver(graph), typeof(NumericsQuaternion));

		objectArrayResolver = new Overlap3DResolver(
			BuildShapeResolver(graph),
			positionResolver,
			rotationResolver,
			Mask?.BuildResolver(graph),
			IncludeAreas,
			IgnoreOperand.Build(IgnoreResolver, graph));

		return true;
	}

	private IObjectResolver<Shape3D> BuildShapeResolver(Graph graph)
	{
		// An unset shape resolves to nothing rather than to a default sphere: a query with no shape authored should
		// find nothing and be obvious about it, not quietly find whatever a guessed radius reaches.
		if (Shape is null
			|| !Shape.TryBuildObjectResolver(graph, out IObjectResolver? objectResolver)
			|| objectResolver is not IObjectResolver<Shape3D> shapeResolver)
		{
			return new ShapePicker3DResolver(null);
		}

		return shapeResolver;
	}
}
