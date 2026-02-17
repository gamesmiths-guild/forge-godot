// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Ports;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

/// <summary>
/// Discovers concrete Statescript node types from loaded assemblies using reflection.
/// </summary>
/// <remarks>
/// Provides port layout information for the editor without requiring node instantiation.
/// </remarks>
internal static class StatescriptNodeDiscovery
{
	private static List<NodeTypeInfo>? _cachedNodeTypes;

	/// <summary>
	/// Gets all discovered concrete node types. Results are cached after first discovery.
	/// </summary>
	/// <returns>A read-only list of node type info.</returns>
	internal static IReadOnlyList<NodeTypeInfo> GetDiscoveredNodeTypes()
	{
		_cachedNodeTypes ??= DiscoverNodeTypes();
		return _cachedNodeTypes;
	}

	/// <summary>
	/// Clears the cached discovery results, forcing re-discovery on next access.
	/// </summary>
	internal static void InvalidateCache()
	{
		_cachedNodeTypes = null;
	}

	/// <summary>
	/// Finds the <see cref="NodeTypeInfo"/> for the given runtime type name.
	/// </summary>
	/// <param name="runtimeTypeName">The full type name stored in the resource.</param>
	/// <returns>The matching node type info, or null if not found.</returns>
	internal static NodeTypeInfo? FindByRuntimeTypeName(string runtimeTypeName)
	{
		IReadOnlyList<NodeTypeInfo> types = GetDiscoveredNodeTypes();

		for (var i = 0; i < types.Count; i++)
		{
			if (types[i].RuntimeTypeName == runtimeTypeName)
			{
				return types[i];
			}
		}

		return null;
	}

	private static List<NodeTypeInfo> DiscoverNodeTypes()
	{
		var results = new List<NodeTypeInfo>();

		Type actionNodeType = typeof(ActionNode);
		Type conditionNodeType = typeof(ConditionNode);
		Type stateNodeOpenType = typeof(StateNode<>);

		// Scan all loaded assemblies for concrete node types.
		foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			Type[] types;
			try
			{
				types = assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException ex)
			{
				types = ex.Types.Where(x => x is not null).ToArray()!;
			}

			foreach (Type type in types)
			{
				if (type.IsAbstract || type.IsGenericTypeDefinition)
				{
					continue;
				}

				// Skip the built-in Entry/Exit nodes — they are handled separately.
				if (type == typeof(EntryNode) || type == typeof(ExitNode))
				{
					continue;
				}

				if (actionNodeType.IsAssignableFrom(type))
				{
					results.Add(BuildNodeTypeInfo(type, StatescriptNodeType.Action));
				}
				else if (conditionNodeType.IsAssignableFrom(type))
				{
					results.Add(BuildNodeTypeInfo(type, StatescriptNodeType.Condition));
				}
				else if (IsConcreteStateNode(type, stateNodeOpenType))
				{
					results.Add(BuildNodeTypeInfo(type, StatescriptNodeType.State));
				}
			}
		}

		results.Sort((a, b) => string.CompareOrdinal(a.DisplayName, b.DisplayName));
		return results;
	}

	private static bool IsConcreteStateNode(Type type, Type stateNodeOpenType)
	{
		Type? current = type.BaseType;
		while (current is not null)
		{
			if (current.IsGenericType && current.GetGenericTypeDefinition() == stateNodeOpenType)
			{
				return true;
			}

			current = current.BaseType;
		}

		return false;
	}

	private static NodeTypeInfo BuildNodeTypeInfo(Type type, StatescriptNodeType nodeType)
	{
		var displayName = FormatDisplayName(type.Name);
		var runtimeTypeName = type.FullName!;

		// Get constructor parameter names.
		var constructorParamNames = GetConstructorParameterNames(type);

		// Determine ports and description by instantiating a temporary node.
		string[] inputLabels;
		string[] outputLabels;
		bool[] isSubgraph;
		string description;

		try
		{
			Node tempNode = CreateTemporaryNode(type);
			inputLabels = GetInputPortLabels(tempNode, nodeType);
			outputLabels = GetOutputPortLabels(tempNode, nodeType);
			isSubgraph = GetSubgraphFlags(tempNode);
			description = tempNode.Description;
		}
		catch
		{
			// Fallback to default port layout based on base type.
			PortLayout[] portLayouts = GetDefaultPortLayout(nodeType);
			inputLabels = [.. portLayouts.Select(x => x.InputLabel)];
			outputLabels = [.. portLayouts.Select(x => x.OutputLabel)];
			isSubgraph = [.. portLayouts.Select(x => x.IsSubgraph)];
			description = $"A {displayName} node.";
		}

		return new NodeTypeInfo(
			displayName,
			runtimeTypeName,
			nodeType,
			inputLabels,
			outputLabels,
			isSubgraph,
			constructorParamNames,
			description);
	}

	private static Node CreateTemporaryNode(Type type)
	{
		// Try to find the primary constructor or the one with the fewest parameters.
		ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

		if (constructors.Length == 0)
		{
			return (Node)Activator.CreateInstance(type)!;
		}

		// Sort by parameter count, prefer the fewest.
		ConstructorInfo constructor = constructors.OrderBy(x => x.GetParameters().Length).First();
		ParameterInfo[] parameters = constructor.GetParameters();

		var args = new object[parameters.Length];
		for (var i = 0; i < parameters.Length; i++)
		{
			Type paramType = parameters[i].ParameterType;

			if (paramType == typeof(Forge.Core.StringKey))
			{
				args[i] = new Forge.Core.StringKey("_placeholder_");
			}
			else if (paramType == typeof(string))
			{
				args[i] = string.Empty;
			}
			else if (paramType.IsValueType)
			{
				args[i] = Activator.CreateInstance(paramType)!;
			}
			else
			{
				args[i] = null!;
			}
		}

		return (Node)constructor.Invoke(args);
	}

	private static string[] GetConstructorParameterNames(Type type)
	{
		ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

		if (constructors.Length == 0)
		{
			return [];
		}

		// Use the constructor with the most parameters (primary constructor).
		ConstructorInfo constructor = constructors.OrderByDescending(x => x.GetParameters().Length).First();
		return [.. constructor.GetParameters().Select(x => x.Name ?? string.Empty)];
	}

	private static string[] GetInputPortLabels(Node node, StatescriptNodeType nodeType)
	{
		var count = node.InputPorts.Length;
		var labels = new string[count];

		switch (nodeType)
		{
			case StatescriptNodeType.Action:
				if (count >= 1)
				{
					labels[0] = "Execute";
				}

				break;

			case StatescriptNodeType.Condition:
				if (count >= 1)
				{
					labels[0] = "Condition";
				}

				break;

			case StatescriptNodeType.State:
				if (count >= 1)
				{
					labels[0] = "Begin";
				}

				if (count >= 2)
				{
					labels[1] = "Abort";
				}

				for (var i = 2; i < count; i++)
				{
					labels[i] = $"Input {i}";
				}

				break;

			default:
				for (var i = 0; i < count; i++)
				{
					labels[i] = $"Input {i}";
				}

				break;
		}

		return labels;
	}

	private static string[] GetOutputPortLabels(Node node, StatescriptNodeType nodeType)
	{
		var count = node.OutputPorts.Length;
		var labels = new string[count];

		switch (nodeType)
		{
			case StatescriptNodeType.Action:
				if (count >= 1)
				{
					labels[0] = "Done";
				}

				break;

			case StatescriptNodeType.Condition:
				if (count >= 1)
				{
					labels[0] = "True";
				}

				if (count >= 2)
				{
					labels[1] = "False";
				}

				break;

			case StatescriptNodeType.State:
				if (count >= 1)
				{
					labels[0] = "OnActivate";
				}

				if (count >= 2)
				{
					labels[1] = "OnDeactivate";
				}

				if (count >= 3)
				{
					labels[2] = "OnAbort";
				}

				if (count >= 4)
				{
					labels[3] = "Subgraph";
				}

				for (var i = 4; i < count; i++)
				{
					labels[i] = $"Event {i}";
				}

				break;

			default:
				for (var i = 0; i < count; i++)
				{
					labels[i] = $"Output {i}";
				}

				break;
		}

		return labels;
	}

	private static bool[] GetSubgraphFlags(Node node)
	{
		var count = node.OutputPorts.Length;
		var flags = new bool[count];

		for (var i = 0; i < count; i++)
		{
			flags[i] = node.OutputPorts[i] is SubgraphPort;
		}

		return flags;
	}

	private static PortLayout[] GetDefaultPortLayout(
		StatescriptNodeType nodeType)
	{
		return nodeType switch
		{
			StatescriptNodeType.Action => [new PortLayout("Execute", "Done", false)],
			StatescriptNodeType.Condition => [
				new PortLayout("Condition", "True", false),
				new PortLayout(string.Empty, "False", false)],
			StatescriptNodeType.State => [
				new PortLayout("Begin", "OnActivate", false),
				new PortLayout("Abort", "OnDeactivate", false),
				new PortLayout(string.Empty, "OnAbort", false),
				new PortLayout(string.Empty, "Subgraph", true)],
			StatescriptNodeType.Entry => throw new NotImplementedException(),
			StatescriptNodeType.Exit => throw new NotImplementedException(),
			_ => [new PortLayout("Input", "Output", false)],
		};
	}

	private static string FormatDisplayName(string typeName)
	{
		// Remove common suffixes.
		if (typeName.EndsWith("Node", StringComparison.Ordinal))
		{
			typeName = typeName[..^4];
		}

		// Insert spaces before capital letters for camelCase names.
		var result = new System.Text.StringBuilder();
		for (var i = 0; i < typeName.Length; i++)
		{
			if (i > 0 && char.IsUpper(typeName[i]) && !char.IsUpper(typeName[i - 1]))
			{
				result.Append(' ');
			}

			result.Append(typeName[i]);
		}

		return result.ToString();
	}

	/// <summary>
	/// Describes a discovered concrete node type and its port layout.
	/// </summary>
	internal sealed class NodeTypeInfo
	{
		/// <summary>
		/// Gets the display name for this node type (e.g., "Timer", "Set Variable", "Expression").
		/// </summary>
		public string DisplayName { get; }

		/// <summary>
		/// Gets the CLR type name used for serialization (typically the type's full name).
		/// </summary>
		public string RuntimeTypeName { get; }

		/// <summary>
		/// Gets the node category (Action, Condition, State).
		/// </summary>
		public StatescriptNodeType NodeType { get; }

		/// <summary>
		/// Gets the input port labels for this node type.
		/// </summary>
		public string[] InputPortLabels { get; }

		/// <summary>
		/// Gets the output port labels for this node type.
		/// </summary>
		public string[] OutputPortLabels { get; }

		/// <summary>
		/// Gets whether each output port is a subgraph port.
		/// </summary>
		public bool[] IsSubgraphPort { get; }

		/// <summary>
		/// Gets the constructor parameter names for this node type.
		/// </summary>
		public string[] ConstructorParameterNames { get; }

		/// <summary>
		/// Gets a brief description for this node type, shown in the Add Node dialog.
		/// Read from the <see cref="Node.Description"/> property at discovery time.
		/// </summary>
		public string Description { get; }

		public NodeTypeInfo(
			string displayName,
			string runtimeTypeName,
			StatescriptNodeType nodeType,
			string[] inputPortLabels,
			string[] outputPortLabels,
			bool[] isSubgraphPort,
			string[] constructorParameterNames,
			string description)
		{
			DisplayName = displayName;
			RuntimeTypeName = runtimeTypeName;
			NodeType = nodeType;
			InputPortLabels = inputPortLabels;
			OutputPortLabels = outputPortLabels;
			IsSubgraphPort = isSubgraphPort;
			ConstructorParameterNames = constructorParameterNames;
			Description = description;
		}
	}

	private record struct PortLayout(string InputLabel, string OutputLabel, bool IsSubgraph);
}
#endif
