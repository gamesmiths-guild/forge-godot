// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that holds a constant <see langword="int"/> picked by name from a
/// <see cref="ForgeStatescriptEnum"/>.
/// </summary>
/// <remarks>
/// The enum reference is authoring metadata: what this resolver contributes to the graph is the member's ordinal value
/// as a plain integer constant, identical to a <see cref="VariantResolverResource"/> holding the same number.
/// </remarks>
[Tool]
[GlobalClass]
public partial class EnumConstantResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "EnumConstant";

	/// <summary>
	/// Gets or sets the enum the value is picked from.
	/// </summary>
	[Export]
	public ForgeStatescriptEnum? EnumDefinition { get; set; }

	/// <summary>
	/// Gets or sets the selected member's value.
	/// </summary>
	[Export]
	public int Value { get; set; }

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		DefineAndBindInputProperty(graph, runtimeNode, $"__enum_{nodeId}_{index}", index, BuildResolver(graph));
	}

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		return new VariantResolver(new Variant128(Value), typeof(int));
	}
}
