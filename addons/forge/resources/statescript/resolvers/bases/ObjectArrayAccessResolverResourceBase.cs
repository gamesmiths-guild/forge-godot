// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;

/// <summary>
/// Base resource for object-lane array access resolvers (first, last, element-at) that read a single element from a
/// nested object array source of any registered object type. Binds the produced element to object-typed node inputs and
/// exposes it as a nested object operand for composition (validity/equality checks, appended elements, etc.).
/// </summary>
[Tool]
public abstract partial class ObjectArrayAccessResolverResourceBase : StatescriptResolverResource
{
	/// <summary>
	/// Gets the prefix used for generated property names when binding to node inputs.
	/// </summary>
	public abstract string PropertyNamePrefix { get; }

	/// <summary>
	/// Builds the core object access resolver over the resolved source array.
	/// </summary>
	/// <param name="graph">The runtime graph being built.</param>
	/// <param name="source">The resolved object array source.</param>
	/// <returns>The scalar object resolver reading the selected element.</returns>
	public abstract IObjectResolver BuildObjectAccessResolver(Graph graph, IObjectArrayResolver source);

	/// <summary>
	/// Gets or sets the nested resolver providing the source object array.
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
		if (!TryBuildObjectResolver(graph, out IObjectResolver? objectResolver) || objectResolver is null)
		{
			return;
		}

		var propertyName = new StringKey($"__{PropertyNamePrefix}_{nodeId}_{index}");
		graph.VariableDefinitions.DefineObjectProperty(propertyName, objectResolver);
		runtimeNode.BindInput(index, propertyName);
	}

	/// <inheritdoc/>
	public override bool TryBuildObjectResolver(Graph graph, out IObjectResolver? objectResolver)
	{
		objectResolver = null;

		if (!ArrayResolverResourceUtilities.TryResolveObjectArraySource(
			Source,
			ResolverTypeId,
			graph,
			out IObjectArrayResolver? source)
			|| source is null)
		{
			return false;
		}

		objectResolver = BuildObjectAccessResolver(graph, source);
		return true;
	}
}
