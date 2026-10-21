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
	/// It also runs while the tree is paused, for entities that keep advancing then.
	/// </remarks>
	public ForgeBootstrap()
	{
		ProcessPhysicsPriority = int.MinValue;
		ProcessMode = ProcessModeEnum.Always;
	}

	public override void _Ready()
	{
		_ = new ForgeManagers(ForgeTagsSource.LoadRegisteredTags());

		// Cue handlers wait for the end of the frame and see only what the frame changed. The highest priority
		// processes last, after every entity has ticked and updated.
		ForgeManagers.Instance.CuesManager.DeferDispatch = true;
		ProcessPriority = int.MaxValue;
	}

	public override void _Process(double delta)
	{
		ForgeManagers.Instance.CuesManager.PresentCues();
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
