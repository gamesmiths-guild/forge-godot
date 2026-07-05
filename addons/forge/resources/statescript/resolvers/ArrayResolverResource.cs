// Copyright © Gamesmiths Guild.

using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using Godot.Collections;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that composes an array value by evaluating a nested resolver for each element.
/// </summary>
[Tool]
[GlobalClass]
public partial class ArrayResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "Array";

	/// <summary>
	/// Gets or sets the nested resolver resource for each array element.
	/// </summary>
	[Export]
	public Array<StatescriptResolverResource> ElementResolvers { get; set; } = [];

	/// <summary>
	/// Gets or sets the authored element type for this array.
	/// </summary>
	[Export]
	public StatescriptVariableType ElementType { get; set; } = StatescriptVariableType.Int;

	/// <summary>
	/// Gets or sets a value indicating whether <see cref="ElementType"/> was explicitly authored.
	/// </summary>
	[Export]
	public bool HasExplicitElementType { get; set; }

	/// <summary>
	/// Gets or sets the registered object variable type id when this array holds object-backed (reference) elements;
	/// empty for value-typed arrays. When set, it takes precedence over <see cref="ElementType"/>.
	/// </summary>
	[Export]
	public string ObjectElementTypeId { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets a value indicating whether the overall array section is expanded in the editor.
	/// </summary>
	[Export]
	public bool IsExpanded { get; set; }

	/// <summary>
	/// Gets or sets the per-element fold states used by the editor UI.
	/// </summary>
	[Export]
	public Array<bool> ElementFoldedStates { get; set; } = [];

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		if (index >= runtimeNode.InputProperties.Length)
		{
			return;
		}

		if (!TryResolveElementType(graph, runtimeNode, index, out Type elementType))
		{
			Type expectedType = runtimeNode.InputProperties[index].ExpectedType;
			GD.PushError(
				$"Statescript: Array resolver '{ResolverTypeId}' can only bind to array-typed inputs, dynamic object " +
				$"inputs, or scalar inputs whose value type matches the authored array elements. Input index {index} " +
				$"on node '{runtimeNode.GetType().Name}' expects '{expectedType}'.");
			return;
		}

		var propertyName = new StringKey($"__array_{nodeId}_{index}");

		if (StatescriptObjectVariableTypeRegistry.TryGetByClrType(
			elementType,
			out StatescriptObjectVariableType? descriptor))
		{
			IObjectArrayResolver? resolver = BuildObjectArrayResolver(graph, descriptor);
			if (resolver is null)
			{
				return;
			}

			graph.VariableDefinitions.DefineObjectArrayProperty(propertyName, resolver);
		}
		else
		{
			ArrayResolver? resolver = BuildValueArrayResolver(graph, elementType);
			if (resolver is null)
			{
				return;
			}

			graph.VariableDefinitions.DefineArrayProperty(propertyName, resolver);
		}

		runtimeNode.BindInput(index, propertyName);
	}

	/// <inheritdoc/>
	public override bool TryBuildArrayResolver(
		Graph graph,
		out IArrayPropertyResolver? valueArrayResolver,
		out IObjectArrayResolver? objectArrayResolver)
	{
		valueArrayResolver = null;
		objectArrayResolver = null;

		if (!TryGetConfiguredElementType(graph, out Type elementType))
		{
			return false;
		}

		if (StatescriptObjectVariableTypeRegistry.TryGetByClrType(
			elementType,
			out StatescriptObjectVariableType? descriptor))
		{
			objectArrayResolver = BuildObjectArrayResolver(graph, descriptor);
			return objectArrayResolver is not null;
		}

		valueArrayResolver = BuildValueArrayResolver(graph, elementType);
		return valueArrayResolver is not null;
	}

	private bool TryResolveElementType(Graph graph, ForgeNode runtimeNode, byte index, out Type elementType)
	{
		Type expectedType = runtimeNode.InputProperties[index].ExpectedType;
		if (expectedType.IsArray && expectedType.GetElementType() is Type runtimeElementType)
		{
			elementType = runtimeElementType;
			return true;
		}

		if (!TryGetConfiguredElementType(graph, out Type configuredElementType))
		{
			elementType = null!;
			return false;
		}

		if (expectedType == typeof(object)
			|| expectedType.IsAssignableFrom(configuredElementType)
			|| StatescriptNumericCompatibility.CanCoerce(configuredElementType, expectedType))
		{
			elementType = configuredElementType;
			return true;
		}

		elementType = null!;
		return false;
	}

	private bool TryGetConfiguredElementType(Graph graph, out Type elementType)
	{
		if (!string.IsNullOrEmpty(ObjectElementTypeId)
			&& StatescriptObjectVariableTypeRegistry.TryGet(
				ObjectElementTypeId,
				out StatescriptObjectVariableType? descriptor))
		{
			elementType = descriptor.ClrType;
			return true;
		}

		if (TryInferElementTypeFromResolvers(graph, out Type inferredElementType))
		{
			elementType = inferredElementType;
			return true;
		}

		if (HasExplicitElementType)
		{
			elementType = StatescriptVariableTypeConverter.ToSystemType(ElementType);
			return true;
		}

		elementType = null!;
		return false;
	}

	private bool TryInferElementTypeFromResolvers(Graph graph, out Type elementType)
	{
		if (ElementResolvers.Count == 0)
		{
			elementType = null!;
			return false;
		}

		StatescriptResolverResource firstResolver = ElementResolvers[0];

		if (firstResolver is VariableResolverResource variableResolver)
		{
			elementType = StatescriptVariableTypeConverter.ToSystemType(variableResolver.VariableType);
			return true;
		}

		if (firstResolver is VariantResolverResource variantResolver)
		{
			elementType = StatescriptVariableTypeConverter.ToSystemType(variantResolver.ValueType);
			return true;
		}

		if (firstResolver is EntityResolverResourceBase)
		{
			elementType = typeof(IForgeEntity);
			return true;
		}

		elementType = firstResolver.BuildResolver(graph).ValueType;
		return true;
	}

	private ArrayResolver? BuildValueArrayResolver(Graph graph, Type elementType)
	{
		var resolvers = new List<IPropertyResolver>(ElementResolvers.Count);

		for (int i = 0; i < ElementResolvers.Count; i++)
		{
			StatescriptResolverResource elementResource = ElementResolvers[i];

			if (elementResource is EntityResolverResourceBase && elementResource is not VariableResolverResource)
			{
				GD.PushError(
					$"Statescript: Array resolver element {i} uses resolver '{elementResource.ResolverTypeId}', " +
					$"which only supports entity references and cannot be used in a '{elementType.Name}[]' array.");
				return null;
			}

			IPropertyResolver resolver = elementResource.BuildResolver(graph);
			resolvers.Add(AdaptResolverForExpectedType(resolver, elementType));
		}

		return new ArrayResolver(elementType, [.. resolvers]);
	}

	private IObjectArrayResolver? BuildObjectArrayResolver(Graph graph, StatescriptObjectVariableType descriptor)
	{
		var resolvers = new List<IObjectResolver>(ElementResolvers.Count);

		for (int i = 0; i < ElementResolvers.Count; i++)
		{
			if (!ElementResolvers[i].TryBuildObjectResolver(graph, out IObjectResolver? elementResolver)
				|| elementResolver is null)
			{
				GD.PushError(
					$"Statescript: Array resolver element {i} uses resolver " +
					$"'{ElementResolvers[i].ResolverTypeId}', which does not produce a '{descriptor.DisplayName}' " +
					"value.");
				return null;
			}

			resolvers.Add(elementResolver);
		}

		return ArrayResolverResourceUtilities.CreateObjectArrayComposite(descriptor.ClrType, resolvers);
	}
}
