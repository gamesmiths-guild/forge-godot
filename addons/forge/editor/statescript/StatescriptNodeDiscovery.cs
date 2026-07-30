// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Gamesmiths.Forge.Godot.Core.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Ports;
using Godot;
using ForgeNode = Gamesmiths.Forge.Statescript.Node;
using GodotDictionary = Godot.Collections.Dictionary<string, Godot.Variant>;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

/// <summary>
/// Discovers concrete Statescript node types from loaded assemblies using reflection.
/// </summary>
/// <remarks>
/// Provides the port layout, description and parameter metadata the editor needs. Layouts come from a node instance
/// built through <see cref="StatescriptNodeFactory"/>, the same path the graph builder uses, so the ports drawn in the
/// editor always match the ports of the built graph.
/// </remarks>
internal static class StatescriptNodeDiscovery
{
	private static readonly Dictionary<string, NodeTypeInfo> _configuredLayoutCache = [];

	private static List<NodeTypeInfo>? _cachedNodeTypes;

	/// <summary>
	/// Gets all discovered concrete node types, each with the port layout of its default configuration. Results are
	/// cached after first discovery.
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
		_configuredLayoutCache.Clear();
	}

	/// <summary>
	/// Finds the <see cref="NodeTypeInfo"/> describing a specific node resource, honoring the constructor arguments
	/// stored in its <c>CustomData</c>.
	/// </summary>
	/// <remarks>
	/// Nodes whose constructor arguments change their port layout (<c>SwitchNode</c>'s case count,
	/// <c>StateMachineNode</c>'s state count) report the layout of that specific configuration rather than the type's
	/// default one. Results are cached per configuration.
	/// </remarks>
	/// <param name="nodeResource">The node resource to describe.</param>
	/// <returns>The matching node type info, or <see langword="null"/> if the runtime type is not discovered.</returns>
	internal static NodeTypeInfo? FindForNode(StatescriptNode nodeResource)
	{
		return FindForConfiguration(nodeResource.RuntimeTypeName, nodeResource.CustomData);
	}

	/// <summary>
	/// Finds the <see cref="NodeTypeInfo"/> a node of the given runtime type would have with the given custom data.
	/// </summary>
	/// <param name="runtimeTypeName">The full type name stored in the resource.</param>
	/// <param name="customData">The custom data holding the node's constructor arguments.</param>
	/// <returns>The matching node type info, or <see langword="null"/> if the runtime type is not discovered.</returns>
	internal static NodeTypeInfo? FindForConfiguration(string runtimeTypeName, GodotDictionary customData)
	{
		NodeTypeInfo? defaultInfo = FindByRuntimeTypeName(runtimeTypeName);

		if (defaultInfo is null || defaultInfo.ConstructorParameterNames.Length == 0)
		{
			return defaultInfo;
		}

		string signature = BuildConfigurationSignature(defaultInfo, customData);

		if (signature.Length == 0)
		{
			return defaultInfo;
		}

		string cacheKey = $"{runtimeTypeName}#{signature}";

		if (_configuredLayoutCache.TryGetValue(cacheKey, out NodeTypeInfo? cached))
		{
			return cached;
		}

		Type? nodeType = StatescriptNodeFactory.ResolveType(runtimeTypeName);

		if (nodeType is null)
		{
			return defaultInfo;
		}

		NodeTypeInfo configured = BuildNodeTypeInfo(nodeType, defaultInfo.NodeType, customData);
		_configuredLayoutCache[cacheKey] = configured;
		return configured;
	}

	/// <summary>
	/// Finds the <see cref="NodeTypeInfo"/> for the given runtime type name.
	/// </summary>
	/// <param name="runtimeTypeName">The full type name stored in the resource.</param>
	/// <returns>The matching node type info, or null if not found.</returns>
	internal static NodeTypeInfo? FindByRuntimeTypeName(string runtimeTypeName)
	{
		IReadOnlyList<NodeTypeInfo> types = GetDiscoveredNodeTypes();

		for (int i = 0; i < types.Count; i++)
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

		Type flowNodeType = typeof(ForgeNode);
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
				else if (flowNodeType.IsAssignableFrom(type))
				{
					// Nodes deriving straight from the base Node define their own port layout (SwitchNode and any
					// custom flow node), so they get their own palette category instead of being forced into an
					// archetype.
					results.Add(BuildNodeTypeInfo(type, StatescriptNodeType.Flow));
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

	private static string BuildConfigurationSignature(NodeTypeInfo defaultInfo, GodotDictionary customData)
	{
		var signature = new StringBuilder();

		// Only constructor arguments can change the layout; the rest of CustomData (fold states, custom width, editor-
		// only settings) must not fragment the cache.
		foreach (string parameterName in defaultInfo.ConstructorParameterNames)
		{
			if (!customData.TryGetValue(parameterName, out Variant value))
			{
				continue;
			}

			signature.Append(parameterName).Append('=').Append(value.ToString()).Append(';');
		}

		return signature.ToString();
	}

	private static NodeTypeInfo BuildNodeTypeInfo(Type type, StatescriptNodeType nodeType)
	{
		return BuildNodeTypeInfo(type, nodeType, customData: null);
	}

	private static NodeTypeInfo BuildNodeTypeInfo(Type type, StatescriptNodeType nodeType, GodotDictionary? customData)
	{
		string displayName = FormatDisplayName(type.Name);
		string runtimeTypeName = type.FullName!;

		// Get constructor parameter names.
		string[] constructorParamNames = StatescriptNodeFactory.GetConstructorParameterNames(type);

		// Determine ports and description by instantiating a temporary node.
		string[] inputLabels;
		string[] outputLabels;
		bool[] isSubgraph;
		string description;
		InputPropertyInfo[] inputPropertiesInfo;
		OutputVariableInfo[] outputVariablesInfo;

		try
		{
			ForgeNode tempNode = StatescriptNodeFactory.Create(type, customData);
			inputLabels = GetInputPortLabels(tempNode, nodeType);
			outputLabels = GetOutputPortLabels(tempNode, nodeType);
			isSubgraph = GetSubgraphFlags(tempNode);
			description = tempNode.Description;
			inputPropertiesInfo = GetInputPropertiesInfo(tempNode);
			outputVariablesInfo = GetOutputVariablesInfo(tempNode);
		}
		catch
		{
			// Fallback to default port layout based on base type.
			PortLayout[] portLayouts = GetDefaultPortLayout(nodeType);
			inputLabels = [.. portLayouts.Select(x => x.InputLabel)];
			outputLabels = [.. portLayouts.Select(x => x.OutputLabel)];
			isSubgraph = [.. portLayouts.Select(x => x.IsSubgraph)];
			description = $"{displayName} node.";
			inputPropertiesInfo = [];
			outputVariablesInfo = [];
		}

		return new NodeTypeInfo(
			displayName,
			runtimeTypeName,
			nodeType,
			inputLabels,
			outputLabels,
			isSubgraph,
			constructorParamNames,
			description,
			inputPropertiesInfo,
			outputVariablesInfo);
	}

	private static string[] GetInputPortLabels(ForgeNode node, StatescriptNodeType nodeType)
	{
		return GetPortLabels(node.InputPorts, index => GetFallbackInputPortLabel(nodeType, index));
	}

	private static string[] GetOutputPortLabels(ForgeNode node, StatescriptNodeType nodeType)
	{
		return GetPortLabels(node.OutputPorts, index => GetFallbackOutputPortLabel(nodeType, index));
	}

	private static string[] GetPortLabels<TPort>(TPort[] ports, Func<int, string> fallbackLabelFactory)
		where TPort : Port
	{
		string[] labels = new string[ports.Length];

		for (int i = 0; i < ports.Length; i++)
		{
			string? label = ports[i].Label;
			labels[i] = string.IsNullOrWhiteSpace(label)
				? fallbackLabelFactory(i)
				: label;
		}

		return labels;
	}

	private static string GetFallbackInputPortLabel(StatescriptNodeType nodeType, int index)
	{
		return nodeType switch
		{
			StatescriptNodeType.Action when index == 0 => "Execute",
			StatescriptNodeType.Condition when index == 0 => "Condition",
			StatescriptNodeType.State when index == 0 => "Begin",
			StatescriptNodeType.State when index == 1 => "Abort",
			_ => $"Input {index}",
		};
	}

	private static string GetFallbackOutputPortLabel(StatescriptNodeType nodeType, int index)
	{
		return nodeType switch
		{
			StatescriptNodeType.Action when index == 0 => "Done",
			StatescriptNodeType.Condition when index == 0 => "True",
			StatescriptNodeType.Condition when index == 1 => "False",
			StatescriptNodeType.State when index == 0 => "OnActivate",
			StatescriptNodeType.State when index == 1 => "OnDeactivate",
			StatescriptNodeType.State when index == 2 => "OnAbort",
			StatescriptNodeType.State when index == 3 => "Subgraph",
			StatescriptNodeType.State => $"Event {index}",
			_ => $"Output {index}",
		};
	}

	private static bool[] GetSubgraphFlags(ForgeNode node)
	{
		int count = node.OutputPorts.Length;
		bool[] flags = new bool[count];

		for (int i = 0; i < count; i++)
		{
			flags[i] = node.OutputPorts[i] is SubgraphPort;
		}

		return flags;
	}

	private static InputPropertyInfo[] GetInputPropertiesInfo(ForgeNode node)
	{
		var propertiesInfo = new InputPropertyInfo[node.InputProperties.Length];
		for (int i = 0; i < node.InputProperties.Length; i++)
		{
			Type expectedType = node.InputProperties[i].ExpectedType;
			bool isArray = expectedType.IsArray;
			if (isArray && expectedType.GetElementType() is Type elementType)
			{
				expectedType = elementType;
			}

			propertiesInfo[i] = new InputPropertyInfo(
				node.InputProperties[i].Label,
				expectedType,
				isArray,
				node.InputProperties[i].IsOptional);
		}

		return propertiesInfo;
	}

	private static OutputVariableInfo[] GetOutputVariablesInfo(ForgeNode node)
	{
		var variablesInfo = new OutputVariableInfo[node.OutputVariables.Length];
		for (int i = 0; i < node.OutputVariables.Length; i++)
		{
			variablesInfo[i] = new OutputVariableInfo(
				node.OutputVariables[i].Label,
				node.OutputVariables[i].ValueType,
				node.OutputVariables[i].Scope);
		}

		return variablesInfo;
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

			// Flow nodes define their own layout, so a single pass-through pair is all that can be assumed here.
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
		var result = new StringBuilder();
		for (int i = 0; i < typeName.Length; i++)
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

		/// <summary>
		/// Gets the input property declarations for this node type.
		/// </summary>
		public InputPropertyInfo[] InputPropertiesInfo { get; }

		/// <summary>
		/// Gets the output variable declarations for this node type.
		/// </summary>
		public OutputVariableInfo[] OutputVariablesInfo { get; }

		public NodeTypeInfo(
			string displayName,
			string runtimeTypeName,
			StatescriptNodeType nodeType,
			string[] inputPortLabels,
			string[] outputPortLabels,
			bool[] isSubgraphPort,
			string[] constructorParameterNames,
			string description,
			InputPropertyInfo[] inputPropertiesInfo,
			OutputVariableInfo[] outputVariablesInfo)
		{
			DisplayName = displayName;
			RuntimeTypeName = runtimeTypeName;
			NodeType = nodeType;
			InputPortLabels = inputPortLabels;
			OutputPortLabels = outputPortLabels;
			IsSubgraphPort = isSubgraphPort;
			ConstructorParameterNames = constructorParameterNames;
			Description = description;
			InputPropertiesInfo = inputPropertiesInfo;
			OutputVariablesInfo = outputVariablesInfo;
		}
	}

	/// <summary>
	/// Describes an input property declared by a node type.
	/// </summary>
	/// <param name="Label">The human-readable label for this input property.</param>
	/// <param name="ExpectedType">The type the node expects to read.</param>
	/// <param name="IsArray">Whether the input expects an array of values.</param>
	/// <param name="IsOptional">Whether leaving the input unbound is a meaningful authoring choice, mirroring the
	/// runtime <see cref="InputProperty.IsOptional"/>. Such rows offer an explicit <c>(None)</c> entry and start
	/// unbound instead of being seeded with a default resolver.</param>
	internal readonly record struct InputPropertyInfo(
		string Label,
		Type ExpectedType,
		bool IsArray = false,
		bool IsOptional = false);

	/// <summary>
	/// Describes an output variable declared by a node type.
	/// </summary>
	/// <param name="Label">The human-readable label for this output variable.</param>
	/// <param name="ValueType">The type the node writes.</param>
	/// <param name="Scope">The default scope for this output variable.</param>
	internal readonly record struct OutputVariableInfo(string Label, Type ValueType, VariableScope Scope);

	private record struct PortLayout(string InputLabel, string OutputLabel, bool IsSubgraph);
}
#endif
