// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class ArrayVariableResolverResource : StatescriptResolverResource
{
	[Export]
	public string VariableName { get; set; } = string.Empty;

	[Export]
	public VariableScope Scope { get; set; }

	[Export]
	public string SharedVariableSetPath { get; set; } = string.Empty;

	[Export]
	public StatescriptVariableType ValueType { get; set; } = StatescriptVariableType.Int;

	public override string ResolverTypeId => "ArrayVariable";

	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		if (string.IsNullOrEmpty(VariableName))
		{
			return;
		}

		var propertyName = new StringKey($"__array_{nodeId}_{index}");
		var variableName = new StringKey(VariableName);
		Type elementType = ResolveElementType(graph);

		if (Scope == VariableScope.Shared)
		{
			graph.VariableDefinitions.DefineArrayProperty(
				propertyName,
				new ArrayVariableResolver(variableName, elementType, VariableScope.Shared));
			runtimeNode.BindInput(index, propertyName);
			return;
		}

		runtimeNode.BindInput(index, variableName);
	}

	private Type ResolveElementType(Graph graph)
	{
		if (Scope == VariableScope.Shared)
		{
			return StatescriptVariableTypeConverter.ToSystemType(ValueType);
		}

		var key = new StringKey(VariableName);
		foreach (ArrayVariableDefinition definition in graph.VariableDefinitions.ArrayVariableDefinitions)
		{
			if (definition.Name == key)
			{
				return definition.ElementType;
			}
		}

		return StatescriptVariableTypeConverter.ToSystemType(ValueType);
	}
}
