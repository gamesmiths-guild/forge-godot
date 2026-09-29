// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core;
using Godot;

namespace Gamesmiths.Forge.Godot.Tests.Helpers;

/// <summary>
/// A scene root that runs a test's code the moment it is handed its owner, for tests of what a spawned scene can do
/// to the graph that spawned it while it is still being spawned.
/// </summary>
internal sealed partial class TestInstantiationHook : Node3D, IInstantiationReceiver
{
	/// <summary>
	/// Gets or sets the code run when any instance is handed its owner. Tests set it and clear it again.
	/// </summary>
	public static Action? Instantiated { get; set; }

	public void OnInstantiated(IForgeEntity? owner, IForgeEntity? source)
	{
		Instantiated?.Invoke();
	}
}
