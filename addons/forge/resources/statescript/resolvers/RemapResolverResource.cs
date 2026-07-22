// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that remaps a value from an input range to an output range.
/// </summary>
[Tool]
[GlobalClass]
public partial class RemapResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "Remap";

	/// <summary>
	/// Gets or sets the nested resolver providing the value to remap.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Value { get; set; }

	/// <summary>
	/// Gets or sets the nested resolver providing the input range start.
	/// </summary>
	[Export]
	public StatescriptResolverResource? InMin { get; set; }

	/// <summary>
	/// Gets or sets the nested resolver providing the input range end.
	/// </summary>
	[Export]
	public StatescriptResolverResource? InMax { get; set; }

	/// <summary>
	/// Gets or sets the nested resolver providing the output range start.
	/// </summary>
	[Export]
	public StatescriptResolverResource? OutMin { get; set; }

	/// <summary>
	/// Gets or sets the nested resolver providing the output range end.
	/// </summary>
	[Export]
	public StatescriptResolverResource? OutMax { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the result is clamped to the output range.
	/// </summary>
	[Export]
	public bool Clamp { get; set; }

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		DefineAndBindInputProperty(graph, runtimeNode, $"__remap_{nodeId}_{index}", index, BuildResolver(graph));
	}

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		return new RemapResolver(
			BuildOperand(Value, graph),
			BuildOperand(InMin, graph),
			BuildOperand(InMax, graph),
			BuildOperand(OutMin, graph),
			BuildOperand(OutMax, graph),
			Clamp);
	}

	private static IPropertyResolver BuildOperand(StatescriptResolverResource? operand, Graph graph)
	{
		return operand?.BuildResolver(graph) ?? new VariantResolver(default, typeof(int));
	}
}
