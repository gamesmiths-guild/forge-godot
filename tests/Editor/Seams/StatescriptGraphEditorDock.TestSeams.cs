// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;
using GodotCollections = Godot.Collections;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

/// <summary>
/// Entry points used by the editor test suite to drive the dock the way a user would.
/// </summary>
/// <remarks>
/// <para>
/// These exist so the tests exercise the real recording paths - the same methods the UI signals call - rather than a
/// parallel implementation that could drift from what actually runs. They are intentionally thin: each one forwards to
/// the private handler a signal would have invoked.
/// </para>
/// <para>
/// A seam that does not match the real signal sequence produces a test that passes while the product is broken, or the
/// reverse, so keep each one a faithful replay of what the control emits.
/// </para>
/// <para>
/// This is a partial of the shipping dock but lives under <c>tests/</c>, so it compiles only for this repository's own
/// Debug builds and never reaches an installed copy of the plugin.
/// </para>
/// </remarks>
public partial class StatescriptGraphEditorDock
{
	/// <summary>
	/// Adds a node at the origin, as the add-node dialog does.
	/// </summary>
	/// <param name="title">The node's title.</param>
	/// <param name="runtimeTypeName">The runtime type the node builds.</param>
	/// <returns>The new node's id.</returns>
	internal string TestOnlyAddNode(string title, string runtimeTypeName)
	{
		return AddNodeAtPosition(StatescriptNodeType.Action, title, runtimeTypeName, Vector2.Zero);
	}

	/// <summary>
	/// Deletes the given nodes, as the graph edit's delete request does.
	/// </summary>
	/// <param name="nodeIds">The ids of the nodes to delete.</param>
	internal void TestOnlyDeleteNodes(GodotCollections.Array<StringName> nodeIds)
	{
		OnDeleteNodesRequest(nodeIds);
	}

	/// <summary>
	/// Writes a node configuration value through the same path a Settings control uses.
	/// </summary>
	/// <param name="graph">The graph owning the node.</param>
	/// <param name="nodeId">The node to configure.</param>
	/// <param name="key">The CustomData key.</param>
	/// <param name="value">The value to store.</param>
	internal void TestOnlySetNodeConfig(StatescriptGraph graph, string nodeId, string key, Variant value)
	{
		if (TryGetLiveNodeVisual(graph, nodeId, out StatescriptGraphNode? visual))
		{
			visual.TestOnlySetNodeConfig(key, value);
		}
	}

	/// <summary>
	/// Resizes a node through the same path the resize handle uses.
	/// </summary>
	/// <param name="graph">The graph owning the node.</param>
	/// <param name="nodeId">The node to resize.</param>
	/// <param name="width">The new width.</param>
	internal void TestOnlySetNodeWidth(StatescriptGraph graph, string nodeId, float width)
	{
		if (TryGetLiveNodeVisual(graph, nodeId, out StatescriptGraphNode? visual))
		{
			visual.TestOnlySetWidth(width);
		}
	}
}
#endif
