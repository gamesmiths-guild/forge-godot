// Copyright © Gamesmiths Guild.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Godot;
using ForgeNode = Gamesmiths.Forge.Statescript.Node;
using GodotVariant = Godot.Variant;

namespace Gamesmiths.Forge.Godot.Core;

/// <summary>
/// Builds a runtime <see cref="Graph"/> from a serialized <see cref="StatescriptGraph"/> resource.
/// Resolves concrete node types from the Forge DLL and other assemblies using reflection and recreates all connections.
/// </summary>
public static class StatescriptGraphBuilder
{
	/// <summary>
	/// Builds a runtime <see cref="Graph"/> from the given <see cref="StatescriptGraph"/> resource.
	/// </summary>
	/// <param name="graphResource">The serialized graph resource.</param>
	/// <returns>A fully constructed runtime graph ready for execution.</returns>
	/// <exception cref="InvalidOperationException">Thrown when a node type cannot be resolved or instantiated.
	/// </exception>
	public static Graph Build(StatescriptGraph graphResource)
	{
		var graph = new Graph();

		var nodeMap = new Dictionary<string, ForgeNode>();

		foreach (StatescriptNode nodeResource in graphResource.Nodes)
		{
			switch (nodeResource.NodeType)
			{
				case StatescriptNodeType.Entry:
					nodeMap[nodeResource.NodeId] = graph.EntryNode;
					break;

				case StatescriptNodeType.Exit:
					var exitNode = new ExitNode();
					graph.AddNode(exitNode);
					nodeMap[nodeResource.NodeId] = exitNode;
					break;

				default:
					ForgeNode runtimeNode = InstantiateNode(nodeResource);
					graph.AddNode(runtimeNode);
					nodeMap[nodeResource.NodeId] = runtimeNode;
					break;
			}
		}

		foreach (StatescriptConnection connectionResource in graphResource.Connections)
		{
			if (!nodeMap.TryGetValue(connectionResource.FromNode, out ForgeNode? fromNode))
			{
				GD.PushWarning(
					$"Statescript: Connection references unknown source node '{connectionResource.FromNode}'.");
				continue;
			}

			if (!nodeMap.TryGetValue(connectionResource.ToNode, out ForgeNode? toNode))
			{
				GD.PushWarning(
					$"Statescript: Connection references unknown target node '{connectionResource.ToNode}'.");
				continue;
			}

			var outputPortIndex = connectionResource.OutputPort;
			var inputPortIndex = connectionResource.InputPort;

			if (outputPortIndex < 0 || outputPortIndex >= fromNode.OutputPorts.Length)
			{
				GD.PushWarning(
					$"Statescript: Output port index {outputPortIndex} out of range on node " +
					$"'{connectionResource.FromNode}'.");
				continue;
			}

			if (inputPortIndex < 0 || inputPortIndex >= toNode.InputPorts.Length)
			{
				GD.PushWarning(
					$"Statescript: Input port index {inputPortIndex} out of range on node " +
					$"'{connectionResource.ToNode}'.");
				continue;
			}

			var connection = new Connection(
				fromNode.OutputPorts[outputPortIndex],
				toNode.InputPorts[inputPortIndex]);

			graph.AddConnection(connection);
		}

		RegisterGraphVariables(graph, graphResource);
		BindNodeProperties(graph, graphResource, nodeMap);
		ValidateActivationDataProviders(graphResource);

		return graph;
	}

	private static void RegisterGraphVariables(Graph graph, StatescriptGraph graphResource)
	{
		foreach (StatescriptGraphVariable variable in graphResource.Variables)
		{
			if (string.IsNullOrEmpty(variable.VariableName))
			{
				continue;
			}

			Type clrType = StatescriptVariableTypeConverter.ToSystemType(variable.VariableType);

			if (variable.IsArray)
			{
				var initialValues = new Variant128[variable.InitialArrayValues.Count];
				for (var i = 0; i < variable.InitialArrayValues.Count; i++)
				{
					initialValues[i] = StatescriptVariableTypeConverter.GodotVariantToForge(
						variable.InitialArrayValues[i],
						variable.VariableType);
				}

				graph.VariableDefinitions.ArrayVariableDefinitions.Add(
					new ArrayVariableDefinition(
						new StringKey(variable.VariableName),
						initialValues,
						clrType));
			}
			else
			{
				Variant128 initialValue = StatescriptVariableTypeConverter.GodotVariantToForge(
					variable.InitialValue,
					variable.VariableType);

				graph.VariableDefinitions.VariableDefinitions.Add(
					new VariableDefinition(
						new StringKey(variable.VariableName),
						initialValue,
						clrType));
			}
		}
	}

	private static void BindNodeProperties(
		Graph graph,
		StatescriptGraph graphResource,
		Dictionary<string, ForgeNode> nodeMap)
	{
		foreach (StatescriptNode nodeResource in graphResource.Nodes)
		{
			if (!nodeMap.TryGetValue(nodeResource.NodeId, out ForgeNode? runtimeNode))
			{
				continue;
			}

			foreach (StatescriptNodeProperty binding in nodeResource.PropertyBindings)
			{
				if (binding.Resolver is null)
				{
					continue;
				}

				var index = (byte)binding.PropertyIndex;

				if (binding.Direction == StatescriptPropertyDirection.Input)
				{
					if (index >= runtimeNode.InputProperties.Length)
					{
						GD.PushWarning(
							$"Statescript: Input property index {index} out of range on node " +
							$"'{nodeResource.NodeId}'.");
						continue;
					}

					binding.Resolver.BindInput(graph, runtimeNode, nodeResource.NodeId, index);
				}
				else
				{
					if (index >= runtimeNode.OutputVariables.Length)
					{
						GD.PushWarning(
							$"Statescript: Output variable index {index} out of range on node " +
							$"'{nodeResource.NodeId}'.");
						continue;
					}

					binding.Resolver.BindOutput(runtimeNode, index);
				}
			}
		}
	}

	private static void ValidateActivationDataProviders(StatescriptGraph graphResource)
	{
		string? firstProvider = null;

		foreach (StatescriptNode node in graphResource.Nodes)
		{
			foreach (StatescriptNodeProperty binding in node.PropertyBindings)
			{
				if (binding.Resolver is ActivationDataResolverResource { ProviderClassName.Length: > 0 } resolver)
				{
					if (firstProvider is null)
					{
						firstProvider = resolver.ProviderClassName;
					}
					else if (resolver.ProviderClassName != firstProvider)
					{
						GD.PushError(
							"Statescript: Graph uses multiple activation data providers " +
							$"('{firstProvider}' and '{resolver.ProviderClassName}'). " +
							"A graph supports only one activation data provider at a time. " +
							"Combine the data into a single provider.");
					}
				}
			}
		}
	}

	private static ForgeNode InstantiateNode(StatescriptNode nodeResource)
	{
		if (string.IsNullOrEmpty(nodeResource.RuntimeTypeName))
		{
			throw new InvalidOperationException(
				$"Node '{nodeResource.NodeId}' of type {nodeResource.NodeType} has no RuntimeTypeName set.");
		}

		Type? nodeType = ResolveType(nodeResource.RuntimeTypeName);

		if (nodeType is null)
		{
			throw new InvalidOperationException(
				$"Could not resolve runtime type '{nodeResource.RuntimeTypeName}' for node " +
				$"'{nodeResource.NodeId}'.");
		}

		ConstructorInfo[] constructors = nodeType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

		if (constructors.Length == 0)
		{
			return (ForgeNode)Activator.CreateInstance(nodeType)!;
		}

		ConstructorInfo constructor = constructors.OrderByDescending(x => x.GetParameters().Length).First();
		ParameterInfo[] parameters = constructor.GetParameters();

		var args = new object[parameters.Length];
		for (var i = 0; i < parameters.Length; i++)
		{
			ParameterInfo param = parameters[i];
			var paramName = param.Name ?? string.Empty;

			if (nodeResource.CustomData.TryGetValue(paramName, out GodotVariant value))
			{
				args[i] = ConvertParameter(value, param.ParameterType);
			}
			else
			{
				args[i] = GetDefaultValue(param.ParameterType);
			}
		}

		return (ForgeNode)constructor.Invoke(args);
	}

	private static Type? ResolveType(string typeName)
	{
		var type = Type.GetType(typeName);
		if (type is not null)
		{
			return type;
		}

		foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			type = assembly.GetType(typeName);
			if (type is not null)
			{
				return type;
			}
		}

		return null;
	}

	private static object ConvertParameter(GodotVariant value, Type targetType)
	{
		if (targetType.IsEnum)
		{
			if (value.VariantType == GodotVariant.Type.Int || value.VariantType == GodotVariant.Type.Float)
			{
				return Enum.ToObject(targetType, value.AsInt32());
			}

			var enumText = value.AsString();
			if (!string.IsNullOrEmpty(enumText))
			{
				return Enum.Parse(targetType, enumText, ignoreCase: true);
			}
		}

		if (targetType == typeof(StringKey))
		{
			return new StringKey(value.AsString());
		}

		if (targetType == typeof(string))
		{
			return value.AsString();
		}

		if (targetType == typeof(int))
		{
			return value.AsInt32();
		}

		if (targetType == typeof(float))
		{
			return value.AsSingle();
		}

		if (targetType == typeof(double))
		{
			return value.AsDouble();
		}

		if (targetType == typeof(bool))
		{
			return value.AsBool();
		}

		if (targetType == typeof(long))
		{
			return value.AsInt64();
		}

		return Convert.ChangeType(value.AsString(), targetType, CultureInfo.InvariantCulture);
	}

	private static object GetDefaultValue(Type type)
	{
		if (type == typeof(StringKey))
		{
			return new StringKey("_default_");
		}

		if (type == typeof(string))
		{
			return string.Empty;
		}

		if (type.IsValueType)
		{
			return Activator.CreateInstance(type)!;
		}

		return null!;
	}
}
