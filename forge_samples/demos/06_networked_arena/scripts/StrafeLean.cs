// Copyright © Gamesmiths Guild.

using Godot;

namespace Gamesmiths.Forge.Example;

/// <summary>
/// Leans a character into its strafe: an animation tree's blend position follows how fast the body moves sideways.
/// The tree poses the hitboxes, so on the server it's simulation: the body's velocity is the server's, and the pose
/// it gives is the one recorded for a shot that rewinds. Every client blends its copy from the velocity the server
/// sent, and shows the same pose.
/// </summary>
public partial class StrafeLean : Node
{
	/// <summary>
	/// Gets or sets the tree that blends the lean.
	/// </summary>
	[Export]
	public required AnimationTree Tree { get; set; }

	/// <summary>
	/// Gets or sets the body whose velocity leans it.
	/// </summary>
	[Export]
	public required CharacterBody3D Body { get; set; }

	/// <summary>
	/// Gets or sets the sideways speed that leans it all the way, in units per second.
	/// </summary>
	[Export]
	public float FullLeanSpeed { get; set; } = 7f;

	/// <summary>
	/// Gets or sets the tree's blend position parameter.
	/// </summary>
	[Export]
	public string Parameter { get; set; } = "parameters/lean/blend_position";

	/// <inheritdoc/>
	public override void _Ready()
	{
		base._Ready();

		// Before the hitboxes' animators advance in the tick.
		ProcessPhysicsPriority = -3;
	}

	/// <inheritdoc/>
	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		float sideways = Body.Velocity.Dot(Body.GlobalBasis.X) / FullLeanSpeed;
		Tree.Set(Parameter, Mathf.Clamp(sideways, -1, 1));
	}
}
