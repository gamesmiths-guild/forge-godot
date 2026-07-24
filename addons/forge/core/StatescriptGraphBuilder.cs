// Copyright © Gamesmiths Guild.

using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Godot;
using ForgeNode = Gamesmiths.Forge.Statescript.Node;

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

			int outputPortIndex = connectionResource.OutputPort;
			int inputPortIndex = connectionResource.InputPort;

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

			if (!string.IsNullOrEmpty(variable.ObjectTypeId)
				&& StatescriptObjectVariableTypeRegistry.TryGet(
					variable.ObjectTypeId,
					out StatescriptObjectVariableType? descriptor))
			{
				var objectVariableName = new StringKey(variable.VariableName);

				if (variable.IsArray)
				{
					descriptor.DefineGraphArrayVariable(graph.VariableDefinitions, objectVariableName);
				}
				else
				{
					descriptor.DefineGraphVariable(graph.VariableDefinitions, objectVariableName);
				}

				continue;
			}

			Type clrType = StatescriptVariableTypeConverter.ToSystemType(variable.VariableType);

			if (variable.IsArray)
			{
				var initialValues = new Variant128[variable.InitialArrayValues.Count];
				for (int i = 0; i < variable.InitialArrayValues.Count; i++)
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

				byte index = (byte)binding.PropertyIndex;

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
				if (binding.Resolver
					is AbilityActivationDataResolverResource { ProviderClassName.Length: > 0 } resolver)
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

		Type? nodeType = StatescriptNodeFactory.ResolveType(nodeResource.RuntimeTypeName);

		if (nodeType is null)
		{
			throw new InvalidOperationException(
				$"Could not resolve runtime type '{nodeResource.RuntimeTypeName}' for node " +
				$"'{nodeResource.NodeId}'.");
		}

		return StatescriptNodeFactory.Create(nodeType, nodeResource.CustomData);
	}
}
