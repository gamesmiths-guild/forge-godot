// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;

/// <summary>
/// Base resource for array-operation resolvers that reduce an array to a scalar value (count, any, sum, element,
/// access, etc.). Handles the nested array source and binding the scalar result to node inputs.
/// </summary>
public abstract partial class ArrayReductionResolverResourceBase : StatescriptResolverResource
{
	/// <summary>
	/// Gets the prefix used for generated property names when binding to node inputs.
	/// </summary>
	public abstract string PropertyNamePrefix { get; }

	/// <summary>
	/// Gets or sets the nested resolver providing the source array.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Source { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the source section is folded in the editor.
	/// </summary>
	[Export]
	public bool SourceFolded { get; set; } = true;

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

	/// <summary>
	/// Resolves the nested array source, reporting an editor error when it is missing or does not produce an array.
	/// </summary>
	/// <param name="graph">The runtime graph being built.</param>
	/// <param name="valueArrayResolver">The value-lane source array resolver, when the source produces one.</param>
	/// <param name="objectArrayResolver">The object-lane source array resolver, when the source produces one.</param>
	/// <returns><see langword="true"/> when the source produced an array.</returns>
	protected bool TryResolveSource(
		Graph graph,
		out IArrayPropertyResolver? valueArrayResolver,
		out IObjectArrayResolver? objectArrayResolver)
	{
		return ArrayResolverResourceUtilities.TryResolveSource(
			Source,
			ResolverTypeId,
			graph,
			out valueArrayResolver,
			out objectArrayResolver);
	}
}
