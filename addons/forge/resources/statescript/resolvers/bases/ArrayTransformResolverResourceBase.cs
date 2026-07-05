// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;

/// <summary>
/// Base resource for array-operation resolvers that produce another array (filter, sort, take, etc.). Handles the
/// nested array source, binding the produced array to array-typed node inputs, and exposing the operation as a nested
/// array source for chained pipelines.
/// </summary>
public abstract partial class ArrayTransformResolverResourceBase : StatescriptResolverResource
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
		if (!TryBuildArrayResolver(
			graph,
			out IArrayPropertyResolver? valueArrayResolver,
			out IObjectArrayResolver? objectArrayResolver))
		{
			return;
		}

		if (index < runtimeNode.InputProperties.Length)
		{
			Type expectedType = runtimeNode.InputProperties[index].ExpectedType;
			Type producedElementType = objectArrayResolver?.ElementType ?? valueArrayResolver!.ElementType;

			bool isCompatible = expectedType == typeof(object)
				|| (expectedType.IsArray
					&& expectedType.GetElementType() is Type expectedElementType
					&& expectedElementType.IsAssignableFrom(producedElementType));

			if (!isCompatible)
			{
				GD.PushError(
					$"Statescript: {ResolverTypeId} resolver produces '{producedElementType.Name}[]', which is not " +
					$"compatible with input index {index} on node '{runtimeNode.GetType().Name}' expecting " +
					$"'{expectedType}'.");
				return;
			}
		}

		var propertyName = new StringKey($"__{PropertyNamePrefix}_{nodeId}_{index}");

		if (objectArrayResolver is not null)
		{
			graph.VariableDefinitions.DefineObjectArrayProperty(propertyName, objectArrayResolver);
		}
		else
		{
			graph.VariableDefinitions.DefineArrayProperty(propertyName, valueArrayResolver!);
		}

		runtimeNode.BindInput(index, propertyName);
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
