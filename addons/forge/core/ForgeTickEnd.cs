// Copyright © Gamesmiths Guild.

using Godot;

namespace Gamesmiths.Forge.Godot.Core;

/// <summary>
/// The last node to run in every physics step: it moves the session clock to the next tick, or to the tick something
/// asked for with <see cref="ForgeManagers.SetNextTick"/>, once every entity has advanced the tick the step began.
/// </summary>
internal sealed partial class ForgeTickEnd : Node
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ForgeTickEnd"/> class.
	/// </summary>
	public ForgeTickEnd()
	{
		ProcessPhysicsPriority = int.MaxValue;
		ProcessMode = ProcessModeEnum.Always;
	}

	/// <inheritdoc/>
	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		ForgeManagers.Instance.EndTick();
	}
}
