// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads the zero-based index of the array element currently being iterated by an enclosing
/// array operation.
/// </summary>
[Tool]
[GlobalClass]
public partial class ElementIndexResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "ElementIndex";

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		DefineAndBindInputProperty(graph, runtimeNode, $"__elementindex_{nodeId}_{index}", index, BuildResolver(graph));
	}

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		return new ElementIndexResolver();
	}
}
