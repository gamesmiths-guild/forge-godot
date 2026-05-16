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

	/// <summary>
	/// Gets or sets which variable bag should be read from or written to.
	/// </summary>
	[Export]
	public VariableScope Scope { get; set; }

	/// <summary>
	/// Gets or sets the resource path of the selected shared variable set. This is editor metadata used for highlighting
	/// and inspector integration when <see cref="Scope"/> is <see cref="VariableScope.Shared"/>.
	/// </summary>
	[Export]
	public string SharedVariableSetPath { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the authored variable type. This is primarily needed for shared-scope lookups whose definitions are
	/// not stored inside the runtime graph.
	/// </summary>
	[Export]
	public StatescriptVariableType VariableType { get; set; } = StatescriptVariableType.Int;

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		if (string.IsNullOrEmpty(VariableName))
		{
			return;
		}

		Type? variableType = FindVariableType(graph, VariableName);
		var variableKey = new StringKey(VariableName);

		if (Scope == VariableScope.Shared
			|| (variableType is not null && NeedsNumericInputAdaptation(runtimeNode, index, variableType)))
		{
			DefineAndBindInputProperty(
				graph,
				runtimeNode,
				$"__var_{nodeId}_{index}",
				index,
				new VariableResolver(variableKey, variableType ?? typeof(int), Scope));
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

		runtimeNode.BindOutput(index, new StringKey(VariableName), Scope);
	}

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		if (string.IsNullOrEmpty(VariableName))
		{
			return new VariantResolver(default, typeof(int));
		}

		Type? variableType = FindVariableType(graph, VariableName);
		return new VariableResolver(new StringKey(VariableName), variableType ?? typeof(int), Scope);
	}

	private Type? FindVariableType(Graph graph, string variableName)
	{
		if (Scope == VariableScope.Shared)
		{
			return StatescriptVariableTypeConverter.ToSystemType(VariableType);
		}

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
