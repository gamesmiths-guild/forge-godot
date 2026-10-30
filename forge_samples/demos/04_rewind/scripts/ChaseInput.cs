// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Nodes;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Example;

/// <summary>
/// Steers its parent <see cref="ForgeEntity"/> toward the player the way a player steers theirs: as input. Every
/// physics tick it submits a frame whose move vector points at the player, the entity's movement component walks the
/// body by it, and the world history keeps the frame like any other.
/// </summary>
public partial class ChaseInput : Node
{
	private ForgeEntity? _entity;
	private Node3D? _body;
	private EntityInput? _input;

	/// <summary>
	/// Gets or sets how close to the player the entity stops.
	/// </summary>
	[Export]
	public float StopDistance { get; set; } = 1.2f;

	/// <summary>
	/// Initializes a new instance of the <see cref="ChaseInput"/> class.
	/// </summary>
	/// <remarks>
	/// It submits before the tick begins, as an input collector does.
	/// </remarks>
	public ChaseInput()
	{
		ProcessPhysicsPriority = int.MinValue;
	}

	public override void _Ready()
	{
		base._Ready();

		_entity = GetParent<ForgeEntity>();
		_body = _entity.GetParent<Node3D>();
		_input = new EntityInput(
			new InputActionSet([], [new InputVector("move_left", "move_right", "move_up", "move_down")]));
		_entity.Input = _input;
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		Vector2 move = Vector2.Zero;

		if (GetTree().GetFirstNodeInGroup("player") is Node3D player)
		{
			Vector3 toPlayer = player.GlobalPosition - _body!.GlobalPosition;
			var flat = new Vector2(toPlayer.X, toPlayer.Z);
			move = flat.Length() > StopDistance ? flat.Normalized() : Vector2.Zero;
		}

		_input!.Submit(default(InputFrame).WithVector(0, new NumericsVector2(move.X, move.Y)));
	}
}
