// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Gamesmiths.Forge.Godot.Editor.Statescript;
using GdUnit4;

namespace Gamesmiths.Forge.Godot.Tests.Statescript.Nodes;

/// <summary>
/// The contract behind the Add Node dialog and, more importantly, behind loading a saved graph: a node resource stores
/// only its runtime type name, and <see cref="StatescriptNodeDiscovery.FindByRuntimeTypeName"/> has to find the node
/// again from that string alone. A moved or renamed node breaks that lookup silently.
/// </summary>
[TestSuite]
public class NodeDiscoveryConformanceTests
{
	/// <summary>
	/// Gets one case per discovered node type, identified by its serialized runtime type name.
	/// </summary>
	public static IEnumerable<object[]> DiscoveredNodes =>
		StatescriptNodeDiscovery.GetDiscoveredNodeTypes()
			.Select(info => new object[] { info.RuntimeTypeName })
			.OrderBy(data => (string)data[0], StringComparer.Ordinal);

	[TestCase]
	[DataPoint(nameof(DiscoveredNodes))]
	[RequireGodotRuntime]
	public void Every_discovered_node_is_findable_by_its_serialized_type_name(string runtimeTypeName)
	{
		StatescriptNodeDiscovery.NodeTypeInfo? found =
			StatescriptNodeDiscovery.FindByRuntimeTypeName(runtimeTypeName);

		found.Should().NotBeNull(
			$"a saved graph stores '{runtimeTypeName}' and has nothing else to find the node by.");
		found!.RuntimeTypeName.Should().Be(runtimeTypeName);
	}

	[TestCase]
	[DataPoint(nameof(DiscoveredNodes))]
	[RequireGodotRuntime]
	public void Every_discovered_node_is_presentable(string runtimeTypeName)
	{
		StatescriptNodeDiscovery.NodeTypeInfo info =
			StatescriptNodeDiscovery.FindByRuntimeTypeName(runtimeTypeName)!;

		info.DisplayName.Should().NotBeNullOrWhiteSpace("the Add Node dialog lists nodes by display name.");
		info.IsSubgraphPort.Should().HaveCount(
			info.OutputPortLabels.Length,
			"the subgraph flags are read positionally against the output ports.");
	}

	[TestCase]
	[RequireGodotRuntime]
	public void Discovered_node_type_names_are_unique()
	{
		IEnumerable<string> duplicates = StatescriptNodeDiscovery.GetDiscoveredNodeTypes()
			.GroupBy(info => info.RuntimeTypeName, StringComparer.Ordinal)
			.Where(group => group.Count() > 1)
			.Select(group => $"{group.Key}: {string.Join(", ", group.Select(info => info.DisplayName))}");

		duplicates.Should().BeEmpty(
			"the runtime type name is what a saved graph stores, so a collision loads the wrong node.");
	}
}
#endif
