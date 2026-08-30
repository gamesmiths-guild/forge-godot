// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;

/// <summary>
/// Base resource for a resolver that produces a value-lane value from its own settings.
/// </summary>
/// <remarks>
/// The whole of such a resolver's binding is defining a property and pointing the input at it, which is identical for
/// every one of them. A concrete resource is then only its exported settings and the one line that builds its runtime
/// counterpart.
/// </remarks>
[Tool]
public abstract partial class ValueResolverResourceBase : StatescriptResolverResource
{
	/// <summary>
	/// Builds the runtime resolver.
	/// </summary>
	/// <param name="graph">The runtime graph being built, for resolvers with nested operands.</param>
	/// <returns>The runtime resolver.</returns>
	protected abstract IPropertyResolver CreateResolver(Graph graph);

	/// <summary>
	/// Gets the prefix used to name this resolver's generated graph property.
	/// </summary>
	protected abstract string PropertyNamePrefix { get; }

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		DefineAndBindInputProperty(
			graph,
			runtimeNode,
			$"__{PropertyNamePrefix}_{nodeId}_{index}",
			index,
			BuildResolver(graph));
	}

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		return CreateResolver(graph);
	}
}
