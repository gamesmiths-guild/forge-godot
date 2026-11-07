// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Resources.Statescript;

/// <summary>
/// A part of a Statescript graph that runs differently in a networked game: client-only, which every machine runs by
/// itself and the server never sends, or server-only, which never runs on a client. The editor shows it as a frame
/// around its nodes, blue for client-only and purple for server-only.
/// </summary>
[Tool]
public partial class StatescriptRegion : Resource
{
	/// <summary>
	/// Gets or sets what the region does to its nodes. <see cref="NodeRegion.None"/> does nothing.
	/// </summary>
	[Export]
	public NodeRegion Kind { get; set; } = NodeRegion.ClientOnly;

	/// <summary>
	/// Gets or sets the ids of the nodes in the region. A node belongs to one region at most.
	/// </summary>
	[Export]
	public Array<string> NodeIds { get; set; } = [];
}
