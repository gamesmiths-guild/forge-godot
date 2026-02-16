// Copyright © Gamesmiths Guild.

using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript;

/// <summary>
/// Represents a connection between two nodes in the Statescript graph.
/// </summary>
[Tool]
public partial class StatescriptConnection : Resource
{
	/// <summary>
	/// Gets or sets the source node id.
	/// </summary>
	[Export]
	public string FromNode { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the source port index.
	/// </summary>
	[Export]
	public int OutputPort { get; set; }

	/// <summary>
	/// Gets or sets the destination node id.
	/// </summary>
	[Export]
	public string ToNode { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the destination port index.
	/// </summary>
	[Export]
	public int InputPort { get; set; }
}
