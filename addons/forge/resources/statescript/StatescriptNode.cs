// Copyright © Gamesmiths Guild.

using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Resources.Statescript;

/// <summary>
/// The type of a Statescript node.
/// </summary>
public enum StatescriptNodeType
{
	/// <summary>
	/// Entry node: single output port. One per graph, cannot be removed. Color: Blue.
	/// </summary>
	Entry = 0,

	/// <summary>
	/// Exit node: single input port. Optional, can have multiple. Color: Blue.
	/// </summary>
	Exit = 1,

	/// <summary>
	/// Action node: one input, one output. Executes an instant action. Color: Green.
	/// </summary>
	Action = 2,

	/// <summary>
	/// Condition node: one input, two outputs (true/false). Color: Yellow.
	/// </summary>
	Condition = 3,

	/// <summary>
	/// State node: two inputs (input/abort), multiple outputs (OnActivate, OnDeactivate, OnAbort, Subgraph, +
	/// custom). Color: Red.
	/// </summary>
	State = 4,
}

/// <summary>
/// Resource representing a single node within a Statescript graph.
/// </summary>
[Tool]
public partial class StatescriptNode : Resource
{
	/// <summary>
	/// Gets or sets the unique identifier for this node within the graph.
	/// </summary>
	[Export]
	public string NodeId { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the display title for this node.
	/// </summary>
	[Export]
	public string Title { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the type of this node.
	/// </summary>
	[Export]
	public StatescriptNodeType NodeType { get; set; }

	/// <summary>
	/// Gets or sets the position of this node in the graph editor.
	/// </summary>
	[Export]
	public Vector2 PositionOffset { get; set; }

	/// <summary>
	/// Gets or sets additional custom data for extended node implementations.
	/// </summary>
	[Export]
	public Dictionary<string, Variant> CustomData { get; set; } = [];
}
