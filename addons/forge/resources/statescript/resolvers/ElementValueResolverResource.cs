// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads the value-typed array element currently being iterated by an enclosing array operation.
/// Use it inside nested predicate, key selector, or projection resolvers as the stand-in for the lambda
/// parameter.
/// </summary>
[Tool]
[GlobalClass]
public partial class ElementValueResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "ElementValue";

	/// <summary>
	/// Gets or sets the authored element type. Must match the iterated array's element type.
	/// </summary>
	[Export]
	public StatescriptVariableType ValueType { get; set; } = StatescriptVariableType.Int;

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		DefineAndBindInputProperty(graph, runtimeNode, $"__elementvalue_{nodeId}_{index}", index, BuildResolver(graph));
	}

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		return new ElementValueResolver(StatescriptVariableTypeConverter.ToSystemType(ValueType));
	}
}
