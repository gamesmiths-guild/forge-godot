// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using ForgeNode = Gamesmiths.Forge.Statescript.Node;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;

/// <summary>
/// Base resource for every resolver that produces a 2D query shape.
/// </summary>
/// <remarks>
/// Shapes travel the object lane, like scenes and nodes do, so the binding plumbing is the same for all of them and
/// lives here. A concrete shape resource only declares its own dimensions and builds its runtime counterpart.
/// </remarks>
[Tool]
public abstract partial class ShapeResolverResourceBase2D : StatescriptResolverResource
{
	/// <summary>
	/// Builds the runtime resolver.
	/// </summary>
	/// <param name="graph">The runtime graph being built, for the nested dimension operands.</param>
	/// <returns>The runtime resolver.</returns>
	protected abstract IObjectResolver<Shape2D> CreateResolver(Graph graph);

	/// <summary>
	/// Gets the prefix used to name this resolver's generated graph property.
	/// </summary>
	protected abstract string PropertyNamePrefix { get; }

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		var propertyName = new StringKey($"__{PropertyNamePrefix}_{nodeId}_{index}");
		graph.VariableDefinitions.DefineObjectProperty(propertyName, CreateResolver(graph));
		runtimeNode.BindInput(index, propertyName);
	}

	/// <inheritdoc/>
	public override bool TryBuildObjectResolver(Graph graph, out IObjectResolver? objectResolver)
	{
		objectResolver = CreateResolver(graph);
		return true;
	}

	/// <summary>
	/// Builds a dimension operand, falling back to zero when none was authored.
	/// </summary>
	/// <param name="resource">The authored operand, when there is one.</param>
	/// <param name="graph">The runtime graph being built.</param>
	/// <returns>The dimension resolver.</returns>
	protected static IPropertyResolver BuildDimension(StatescriptResolverResource? resource, Graph graph)
	{
		return AdaptResolverForExpectedType(
			resource?.BuildResolver(graph) ?? new VariantResolver(new Variant128(0.0), typeof(double)),
			typeof(double));
	}

	/// <summary>
	/// Builds a vector dimension operand, falling back to zero when none was authored.
	/// </summary>
	/// <param name="resource">The authored operand, when there is one.</param>
	/// <param name="graph">The runtime graph being built.</param>
	/// <returns>The dimension resolver.</returns>
	protected static IPropertyResolver BuildVectorDimension(StatescriptResolverResource? resource, Graph graph)
	{
		return AdaptResolverForExpectedType(
			resource?.BuildResolver(graph)
				?? new VariantResolver(new Variant128(NumericsVector2.Zero), typeof(NumericsVector2)),
			typeof(NumericsVector2));
	}
}
