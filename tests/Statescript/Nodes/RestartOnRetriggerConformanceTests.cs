// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Gamesmiths.Forge.Godot.Core.Statescript;
using Gamesmiths.Forge.Godot.Editor.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using GdUnit4;
using ForgeNode = Gamesmiths.Forge.Statescript.Node;
using GodotDictionary = Godot.Collections.Dictionary<string, Godot.Variant>;

namespace Gamesmiths.Forge.Godot.Tests.Statescript.Nodes;

/// <summary>
/// The Restart On Retrigger setting across every state node the editor lists, the plugin's own and the core ones alike.
/// The editor offers the setting from the node's constructor, so a node that can restart has to take the choice there,
/// and the choice the editor stores has to reach the node the graph builds.
/// </summary>
[TestSuite]
public class RestartOnRetriggerConformanceTests
{
	/// <summary>
	/// Gets one case per discovered state node type.
	/// </summary>
	public static IEnumerable<object[]> DiscoveredStateNodes =>
		StatescriptNodeDiscovery.GetDiscoveredNodeTypes()
			.Where(info => info.NodeType == StatescriptNodeType.State)
			.Select(info => new object[] { info.RuntimeTypeName })
			.OrderBy(data => (string)data[0], StringComparer.Ordinal);

	/// <summary>
	/// Gets one case per discovered node type the editor offers the setting on.
	/// </summary>
	public static IEnumerable<object[]> RestartableNodes =>
		StatescriptNodeDiscovery.GetDiscoveredNodeTypes()
			.Where(info => info.CanRestartOnRetrigger)
			.Select(info => new object[] { info.RuntimeTypeName })
			.OrderBy(data => (string)data[0], StringComparer.Ordinal);

	[TestCase]
	[DataPoint(nameof(DiscoveredStateNodes))]
	[RequireGodotRuntime]
	public void A_state_node_is_offered_the_setting_exactly_when_it_can_restart(string runtimeTypeName)
	{
		StatescriptNodeDiscovery.NodeTypeInfo info = StatescriptNodeDiscovery.FindByRuntimeTypeName(runtimeTypeName)!;

		// A node that overrides OnRestart takes the choice through a restartOnRetrigger constructor parameter, which is
		// what the editor reads, and one that does not would be offered a choice that changes nothing.
		info.CanRestartOnRetrigger.Should().Be(
			OverridesRestart(StatescriptNodeFactory.ResolveType(runtimeTypeName)!),
			"the editor offers the setting exactly on the nodes that can restart.");
	}

	[TestCase]
	[DataPoint(nameof(RestartableNodes))]
	[RequireGodotRuntime]
	public void The_setting_reaches_the_built_node(string runtimeTypeName)
	{
		Type type = StatescriptNodeFactory.ResolveType(runtimeTypeName)!;

		ForgeNode node = StatescriptNodeFactory.Create(
			type,
			new GodotDictionary { { StatescriptNodeDiscovery.RestartOnRetriggerKey, true } });

		((bool)type.GetProperty("RestartOnRetrigger")!.GetValue(node)!).Should().BeTrue(
			"the node has to pass the choice on to the base node, or ticking the box changes nothing.");
	}

	private static bool OverridesRestart(Type type)
	{
		Type declaringType = type.GetMethod("OnRestart", BindingFlags.Instance | BindingFlags.NonPublic)!
			.DeclaringType!;

		return !declaringType.IsGenericType || declaringType.GetGenericTypeDefinition() != typeof(StateNode<>);
	}
}
#endif
