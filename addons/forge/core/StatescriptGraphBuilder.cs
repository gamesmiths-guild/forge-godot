// Copyright © Gamesmiths Guild.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Resources.Statescript;
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

		// Map from serialized NodeId to runtime node instance.
		var nodeMap = new Dictionary<string, ForgeNode>();

		// Instantiate all nodes.
		foreach (StatescriptNode nodeResource in graphResource.Nodes)
		{
			switch (nodeResource.NodeType)
			{
				case StatescriptNodeType.Entry:
					// The entry node is always present in the runtime graph.
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

		// Create connections.
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

		return graph;
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

		// Prefer the constructor with the most parameters (primary constructor for records/positional types).
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
				// Use a sensible default if the parameter isn't in CustomData.
				args[i] = GetDefaultValue(param.ParameterType);
			}
		}

		return (ForgeNode)constructor.Invoke(args);
	}

	private static Type? ResolveType(string typeName)
	{
		// Try direct resolution first.
		var type = Type.GetType(typeName);
		if (type is not null)
		{
			return type;
		}

		// Search all loaded assemblies.
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

		// Fallback: try string conversion.
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
