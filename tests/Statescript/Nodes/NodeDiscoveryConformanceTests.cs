// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Gamesmiths.Forge.Godot.Editor.Statescript;
using Gamesmiths.Forge.Godot.Tests.Helpers;
using GdUnit4;

namespace Gamesmiths.Forge.Godot.Tests.Statescript.Nodes;

/// <summary>
/// The contract behind loading a saved graph: a node resource stores only its runtime type name, and
/// <see cref="StatescriptNodeDiscovery.FindByRuntimeTypeName"/> has to find the node again from that string alone.
/// </summary>
/// <remarks>
/// The compatibility half is driven from a committed manifest rather than from discovery itself. Asking discovery for
/// its own type names and then looking those names back up in the same cached list proves nothing - it passes however
/// the types are renamed. The manifest is the record of what already exists in saved graphs, so renaming a node fails
/// against it until the rename is dealt with deliberately.
/// </remarks>
[TestSuite]
public class NodeDiscoveryConformanceTests
{
	private static readonly string _manifestPath = Path.Combine(
		ProjectFiles.ProjectRoot, "tests", "Statescript", "Nodes", "serialized-node-types.txt");

	/// <summary>
	/// Gets one case per node type recorded in the manifest.
	/// </summary>
	public static IEnumerable<object[]> ManifestedNodeTypes =>
		ReadManifest().Select(runtimeTypeName => new object[] { runtimeTypeName });

	/// <summary>
	/// Gets one case per discovered node type, identified by its serialized runtime type name.
	/// </summary>
	public static IEnumerable<object[]> DiscoveredNodes =>
		StatescriptNodeDiscovery.GetDiscoveredNodeTypes()
			.Select(info => new object[] { info.RuntimeTypeName })
			.OrderBy(data => (string)data[0], StringComparer.Ordinal);

	[TestCase]
	[DataPoint(nameof(ManifestedNodeTypes))]
	[RequireGodotRuntime]
	public void Every_manifested_node_type_is_still_loadable(string runtimeTypeName)
	{
		StatescriptNodeDiscovery.NodeTypeInfo? found =
			StatescriptNodeDiscovery.FindByRuntimeTypeName(runtimeTypeName);

		found.Should().NotBeNull(
			$"graphs saved before now store '{runtimeTypeName}'. If this node was renamed or moved, those graphs no "
			+ "longer load; update the manifest only once that is handled.");
	}

	[TestCase]
	[RequireGodotRuntime]
	public void The_manifest_records_every_discovered_node_type()
	{
		HashSet<string> manifested = [.. ReadManifest()];

		IEnumerable<string> unrecorded = StatescriptNodeDiscovery.GetDiscoveredNodeTypes()
			.Select(info => info.RuntimeTypeName)
			.Where(runtimeTypeName => !manifested.Contains(runtimeTypeName))
			.OrderBy(runtimeTypeName => runtimeTypeName, StringComparer.Ordinal);

		unrecorded.Should().BeEmpty(
			$"a new node is not covered against renaming until it is listed. Add these lines to {_manifestPath}.");
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

	private static IEnumerable<string> ReadManifest()
	{
		return File.ReadAllLines(_manifestPath)
			.Select(line => line.Trim())
			.Where(line => line.Length > 0 && !line.StartsWith('#'));
	}
}
#endif
