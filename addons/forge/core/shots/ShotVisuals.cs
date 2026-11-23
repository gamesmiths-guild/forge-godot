// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Shots;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Shots;

// Shows lightweight projectiles: each flight's scene, flown along its path every frame at the tick its entity is shown
// at, and its impact scene once that tick reaches its end. A client shows another player's shot behind, at the tick it
// shows the world at, so the shot leaves that player's hand and meets the end the server sent where it should.
internal sealed partial class ShotVisuals : Node
{
	private const string VisualsName = "ForgeShotVisuals";

	private readonly Dictionary<(EntityShots Shots, ShotId Id), Visual> _visuals = [];
	private readonly List<(EntityShots Shots, ShotId Id)> _done = [];

	public static ShotVisuals? Of(ForgeEntity entity)
	{
		if (!entity.IsInsideTree())
		{
			return null;
		}

		Node parent = entity.GetTree().CurrentScene ?? entity.GetTree().Root;

		if (parent.GetNodeOrNull<ShotVisuals>(VisualsName) is ShotVisuals visuals)
		{
			return visuals;
		}

		visuals = new ShotVisuals { Name = VisualsName };
		parent.AddChild(visuals);
		return visuals;
	}

	public void Show(ForgeEntity entity, in ProjectileFlight flight)
	{
		Node? node = ForgeProjectileData.Of(flight.Projectile)?.Scene?.Instantiate();

		if (node is not null)
		{
			AddChild(node);
		}

		_visuals[(entity.Shots, flight.Id)] = new Visual(entity, node) { Flight = flight };
		Place(_visuals[(entity.Shots, flight.Id)]);
	}

	public void ShowEnd(in ProjectileFlight flight)
	{
		if (flight.Instigator is ForgeEntity entity
			&& _visuals.TryGetValue((entity.Shots, flight.Id), out Visual? visual))
		{
			visual.Flight = flight;
		}
	}

	public void Hide(in ProjectileFlight flight)
	{
		if (flight.Instigator is ForgeEntity entity && _visuals.Remove((entity.Shots, flight.Id), out Visual? visual))
		{
			visual.Node?.QueueFree();
		}
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		_done.Clear();

		foreach (KeyValuePair<(EntityShots Shots, ShotId Id), Visual> visual in _visuals)
		{
			if (!Place(visual.Value))
			{
				_done.Add(visual.Key);
			}
		}

		foreach ((EntityShots Shots, ShotId Id) key in _done)
		{
			_visuals.Remove(key);
		}
	}

	// The tick an entity's projectiles are shown at: a mirror's at the tick its client shows the world at, any other's
	// between its last two ticks, as its body is.
	private static double ShownTick(ForgeEntity entity)
	{
		if (entity.IsMirror && ForgeNetworkSession.Of(entity) is ForgeNetworkSession session)
		{
			return session.ShownTick();
		}

		return ForgeManagers.Instance.Clock.Tick - 2 + Engine.GetPhysicsInterpolationFraction();
	}

	// Flies the visual to its place, or ends it once the shown tick reaches its end: false when it's done.
	private bool Place(Visual visual)
	{
		ProjectileFlight flight = visual.Flight;

		if (!IsInstanceValid(visual.Entity))
		{
			visual.Node?.QueueFree();
			return false;
		}

		double tick = ShownTick(visual.Entity);

		if (!flight.IsFlying && tick >= flight.EndTick)
		{
			visual.Node?.QueueFree();
			ShowImpact(flight);
			return false;
		}

		if (visual.Node is null)
		{
			return true;
		}

		double dt = ForgeManagers.Instance.Clock.FixedDeltaTime;
		double age = (tick - flight.Tick) * dt;
		System.Numerics.Vector3 at = flight.PositionAt(age);
		System.Numerics.Vector3 heading = flight.Velocity + (flight.Projectile.Gravity * (float)age);

		switch (visual.Node)
		{
			case Node3D node3D:
				node3D.Visible = age >= 0;
				node3D.GlobalPosition = new Vector3(at.X, at.Y, at.Z);

				if (heading.LengthSquared() > 0)
				{
					Vector3 forward = new Vector3(heading.X, heading.Y, heading.Z).Normalized();
					Vector3 up = Mathf.Abs(forward.Dot(Vector3.Up)) > 0.99f ? Vector3.Forward : Vector3.Up;
					node3D.GlobalBasis = Basis.LookingAt(forward, up);
				}

				break;

			case Node2D node2D:
				node2D.Visible = age >= 0;
				node2D.GlobalPosition = new Vector2(at.X, at.Y);
				node2D.GlobalRotation = Mathf.Atan2(heading.Y, heading.X);
				break;
		}

		return true;
	}

	private void ShowImpact(in ProjectileFlight flight)
	{
		if (flight.End is not (FlightEnd.Impact or FlightEnd.Struck)
			|| ForgeProjectileData.Of(flight.Projectile)?.ImpactScene?.Instantiate() is not Node impact)
		{
			return;
		}

		AddChild(impact);

		switch (impact)
		{
			case Node3D node3D:
				node3D.GlobalPosition = new Vector3(flight.EndPoint.X, flight.EndPoint.Y, flight.EndPoint.Z);
				break;

			case Node2D node2D:
				node2D.GlobalPosition = new Vector2(flight.EndPoint.X, flight.EndPoint.Y);
				break;
		}
	}

	private sealed class Visual(ForgeEntity entity, Node? node)
	{
		public ForgeEntity Entity { get; } = entity;

		public Node? Node { get; } = node;

		public ProjectileFlight Flight { get; set; }
	}
}
