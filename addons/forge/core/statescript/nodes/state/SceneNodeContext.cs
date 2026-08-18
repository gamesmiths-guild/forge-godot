// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Nodes;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// The context for a <see cref="SceneNode"/>. Holds the instance the node owns so it can free exactly that one
/// on deactivation.
/// </summary>
public class SceneNodeContext : StateNodeContext
{
	/// <summary>
	/// Gets or sets the instance instance, or <see langword="null"/> when nothing was instance.
	/// </summary>
	public Node? Instance { get; set; }

	/// <summary>
	/// Gets or sets how long the instance has existed, in seconds.
	/// </summary>
	public double ElapsedTime { get; set; }

	/// <summary>
	/// Gets or sets the lifetime the instance was given, in seconds. Zero or less means it lives until the node
	/// deactivates.
	/// </summary>
	public double Lifetime { get; set; }
}
