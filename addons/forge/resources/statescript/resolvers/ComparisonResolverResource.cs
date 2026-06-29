// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that compares two nested numeric resolvers and produces a boolean result.
/// </summary>
[Tool]
[GlobalClass]
public partial class ComparisonResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "Comparison";

	/// <summary>
	/// Gets or sets the left-hand operand resolver.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Left { get; set; }

	/// <summary>
	/// Gets or sets the comparison operation.
	/// </summary>
	[Export]
	public ComparisonOperation Operation { get; set; }

	/// <summary>
	/// Gets or sets the right-hand operand resolver.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Right { get; set; }

	[Export]
	public bool LeftFolded { get; set; }

	[Export]
	public bool OperationFolded { get; set; } = true;

	[Export]
	public bool RightFolded { get; set; }

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		IPropertyResolver comparisonResolver = BuildResolver(graph);
		var propertyName = new StringKey($"__cmp_{nodeId}_{index}");

		graph.VariableDefinitions.DefineProperty(propertyName, comparisonResolver);

		runtimeNode.BindInput(index, propertyName);
	}

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		IPropertyResolver leftResolver = Left?.BuildResolver(graph)
			?? new VariantResolver(default, typeof(int));

		IPropertyResolver rightResolver = Right?.BuildResolver(graph)
			?? new VariantResolver(default, typeof(int));

		var operation = (ComparisonOperation)(byte)Operation;

		return new ComparisonResolver(leftResolver, operation, rightResolver);
	}
}
