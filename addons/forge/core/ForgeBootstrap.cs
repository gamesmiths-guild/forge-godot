// Copyright © Gamesmiths Guild.

using System;
using System.Runtime.ExceptionServices;
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
	/// Runs its physics step right after the input collectors and before every other node, so every entity begins its
	/// tick, its random restarted and its input frame taken, before any entity advances, and the frame taken is the one
	/// collected in the same step. It also runs while the tree is paused, for entities that keep advancing then.
	/// </remarks>
	public ForgeBootstrap()
	{
		ProcessPhysicsPriority = int.MinValue + 1;
		ProcessMode = ProcessModeEnum.Always;
	}

	public override void _Ready()
	{
		_ = new ForgeManagers(ForgeTagsSource.LoadRegisteredTags());

		// Abilities and cues are presented at the end of the frame and show only what the frame changed. The highest
		// priority processes last, after every entity has ticked and updated.
		ForgeManagers.Instance.CuesManager.DeferDispatch = true;
		ProcessPriority = int.MaxValue;
	}

	public override void _Process(double delta)
	{
		ExceptionDispatchInfo? failure = null;

		// Presented here rather than by each entity, so an ability one entity started and another's later callback
		// ended within the frame is never shown. An entity whose presentation throws keeps no other from presenting.
		foreach (Node node in GetTree().GetNodesInGroup(ForgeEntity.SimulationGroup))
		{
			if (node is not ForgeEntity entity)
			{
				continue;
			}

			try
			{
				entity.Abilities.PresentAbilities();
			}
			catch (Exception exception)
			{
				failure ??= ExceptionDispatchInfo.Capture(exception);
			}
		}

		ForgeManagers.Instance.CuesManager.PresentCues();
		failure?.Throw();
	}

	public override void _PhysicsProcess(double delta)
	{
		long tick = (long)Engine.GetPhysicsFrames();

		foreach (Node node in GetTree().GetNodesInGroup(ForgeEntity.SimulationGroup))
		{
			// A world history begins the ticks of the entities it advances.
			if (node is ForgeEntity { WorldHistory: null } entity)
			{
				EntitySimulation.BeginTick(entity, tick);
			}
		}
	}
}
