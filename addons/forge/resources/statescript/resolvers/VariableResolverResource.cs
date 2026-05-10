// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that binds a node property to a graph variable by name.
/// </summary>
[Tool]
[GlobalClass]
public partial class VariableResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "Variable";

	/// <summary>
	/// Gets or sets the name of the graph variable to bind to.
	/// </summary>
	[Export]
	public string VariableName { get; set; } = string.Empty;

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		if (string.IsNullOrEmpty(VariableName))
		{
			return;
		}

		Type? variableType = FindGraphVariableType(graph, VariableName);
		var variableKey = new StringKey(VariableName);

		if (variableType is not null && NeedsNumericInputAdaptation(runtimeNode, index, variableType))
		{
			DefineAndBindInputProperty(
				graph,
				runtimeNode,
				$"__var_{nodeId}_{index}",
				index,
				new VariableResolver(variableKey, variableType));
			return;
		}

		runtimeNode.BindInput(index, variableKey);
	}

	/// <inheritdoc/>
	public override void BindOutput(ForgeNode runtimeNode, byte index)
	{
		if (string.IsNullOrEmpty(VariableName))
		{
			return;
		}

		runtimeNode.BindOutput(index, new StringKey(VariableName));
	}

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		if (string.IsNullOrEmpty(VariableName))
		{
			return new VariantResolver(default, typeof(int));
		}

		Type? variableType = FindGraphVariableType(graph, VariableName);
		return new VariableResolver(new StringKey(VariableName), variableType ?? typeof(int));
	}

	private static Type? FindGraphVariableType(Graph graph, string variableName)
	{
		var key = new StringKey(variableName);

		foreach (VariableDefinition def in graph.VariableDefinitions.VariableDefinitions)
		{
			if (def.Name == key)
			{
				return def.ValueType;
			}
		}

		foreach (ArrayVariableDefinition definition in graph.VariableDefinitions.ArrayVariableDefinitions)
		{
			if (definition.Name == key)
			{
				return definition.ElementType;
			}
		}

		return null;
	}
}
