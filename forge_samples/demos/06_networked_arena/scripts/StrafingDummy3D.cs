// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Networking;
using Godot;

namespace Gamesmiths.Forge.Example;

/// <summary>
/// A target dummy that strafes back and forth along a line, timed by the simulation's ticks, where its entity is
/// simulated: a client's copy is placed by the session, so it doesn't move itself.
/// </summary>
public partial class StrafingDummy3D : CharacterBody3D
{
	private ForgeEntity? _entity;
	private Vector3 _origin;

	/// <summary>
	/// Gets or sets the line it strafes along, in the world.
	/// </summary>
	[Export]
	public Vector3 Axis { get; set; } = Vector3.Right;

	/// <summary>
	/// Gets or sets how far it strafes either side of where it was placed.
	/// </summary>
	[Export]
	public float Distance { get; set; } = 3f;

	/// <summary>
	/// Gets or sets how long a strafe there and back takes, in seconds.
	/// </summary>
	[Export]
	public float PeriodSeconds { get; set; } = 3f;

	/// <inheritdoc/>
	public override void _Ready()
	{
		base._Ready();

		_entity = GetNode<ForgeEntity>("Forge Entity");
		_origin = GlobalPosition;
	}

	/// <inheritdoc/>
	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		if (_entity?.IsMirror != false)
		{
			return;
		}

		SimulationClock clock = ForgeManagers.Instance.Clock;
		double angle = Mathf.Tau * clock.Tick * clock.FixedDeltaTime / PeriodSeconds;
		Vector3 axis = Axis.Normalized();

		GlobalPosition = _origin + (axis * Distance * (float)Mathf.Sin(angle));
		Velocity = axis * Distance * (Mathf.Tau / PeriodSeconds) * (float)Mathf.Cos(angle);
	}
}
