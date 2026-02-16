// Copyright © Gamesmiths Guild.

using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Resources.Statescript;

/// <summary>
/// Resource representing a complete Statescript graph. Contains all nodes and their connections.
/// </summary>
[Tool]
[GlobalClass]
[Icon("uid://b6yrjb46fluw3")]
public partial class StatescriptGraph : Resource
{
	/// <summary>
	/// Gets or sets the display name for this graph.
	/// </summary>
	[Export]
	public string GraphName { get; set; } = "New Graph";

	/// <summary>
	/// Gets or sets the nodes in this graph.
	/// </summary>
	[Export]
	public Array<StatescriptNode> Nodes { get; set; } = [];

	/// <summary>
	/// Gets or sets the connections between nodes in this graph.
	/// </summary>
	[Export]
	public Array<StatescriptConnection> Connections { get; set; } = [];

	/// <summary>
	/// Gets or sets the scroll offset of the graph editor when this graph was last saved.
	/// </summary>
	[Export]
	public Vector2 ScrollOffset { get; set; }

	/// <summary>
	/// Gets or sets the zoom level of the graph editor when this graph was last saved.
	/// </summary>
	[Export]
	public float Zoom { get; set; } = 1.0f;

	/// <summary>
	/// Ensures the graph has an Entry node. Called when the graph is first created or loaded.
	/// </summary>
	public void EnsureEntryNode()
	{
		foreach (StatescriptNode node in Nodes)
		{
			if (node.NodeType == StatescriptNodeType.Entry)
			{
				return;
			}
		}

		var entryNode = new StatescriptNode
		{
			NodeId = "entry",
			Title = "Entry",
			NodeType = StatescriptNodeType.Entry,
			PositionOffset = new Vector2(100, 200),
		};

		Nodes.Add(entryNode);
	}
}
