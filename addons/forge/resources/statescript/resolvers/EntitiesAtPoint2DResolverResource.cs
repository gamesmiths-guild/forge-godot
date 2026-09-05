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
/// Resolver resource that reads the entities whose colliders contain a point.
/// </summary>
[Tool]
[GlobalClass]
public partial class EntitiesAtPoint2DResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "EntitiesAtPoint2D";

	/// <summary>
	/// Gets or sets the nested resolver providing the point to test.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Position { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the position section is folded in the editor.
	/// </summary>
	[Export]
	public bool PositionFolded { get; set; }

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
	/// Gets or sets a value indicating whether areas count, as well as bodies.
	/// </summary>
	[Export]
	public bool IncludeAreas { get; set; }

	/// <summary>
	/// Gets or sets the entities left out of the results. Unset leaves out nothing.
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

		var propertyName = new StringKey($"__entitiesatpoint2d_{nodeId}_{index}");
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
			? new EntityPosition2DResolver(new AbilityOwnerResolver(), string.Empty, TransformSpace.Global)
			: AdaptResolverForExpectedType(Position.BuildResolver(graph), typeof(NumericsVector2));

		objectArrayResolver = new EntitiesAtPoint2DResolver(
			positionResolver,
			Mask?.BuildResolver(graph),
			IncludeAreas,
			IgnoreOperand.Build(IgnoreResolver, graph));

		return true;
	}
}
