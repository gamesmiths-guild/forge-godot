// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Godot.Resources;
using Godot;

namespace Gamesmiths.Forge.Godot.Core;

public partial class ForgeBootstrap : Node
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ForgeBootstrap"/> class.
	/// </summary>
	/// <remarks>
	/// Runs its physics step before every other node, so every entity's random restarts before any entity advances.
	/// </remarks>
	public ForgeBootstrap()
	{
		ProcessPhysicsPriority = int.MinValue;
	}

	public override void _Ready()
	{
		_ = new ForgeManagers(ForgeTagsSource.LoadRegisteredTags());
	}

	public override void _PhysicsProcess(double delta)
	{
		long tick = (long)Engine.GetPhysicsFrames();

		foreach (Node node in GetTree().GetNodesInGroup(ForgeEntity.SimulationGroup))
		{
			EntitySimulation.BeginTick((ForgeEntity)node, tick);
		}
	}
}
