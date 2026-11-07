// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Tags;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Example;

/// <summary>
/// An enemy's mind, on the server: it walks toward the nearest player through its entity's input, which its movement
/// component reads, and frees its body once it has fallen. One whose health runs out is freed by its death graph, whose
/// burst every screen shows. A client's mirror does neither: the server's enemy walks, and leaves the client as a
/// despawn.
/// </summary>
public partial class EnemyBrain : Node
{
	private ForgeEntity? _entity;
	private Node3D? _body;
	private EntityInput? _input;
	private Tag _fallen;

	/// <summary>
	/// Gets or sets how close to a player the enemy stops.
	/// </summary>
	[Export]
	public float StopDistance { get; set; } = 1.2f;

	/// <summary>
	/// Initializes a new instance of the <see cref="EnemyBrain"/> class.
	/// </summary>
	/// <remarks>
	/// Steers before the bootstrap begins the tick, so the movement component reads this step's input.
	/// </remarks>
	public EnemyBrain()
	{
		ProcessPhysicsPriority = int.MinValue;
	}

	/// <inheritdoc/>
	public override void _Ready()
	{
		base._Ready();

		_entity = GetParent<ForgeEntity>();
		_body = _entity.GetParent<Node3D>();
		_input = new EntityInput(
			new InputActionSet([], [new InputVector("move_left", "move_right", "move_up", "move_down")]));
		_entity.Input = _input;
		_fallen = Tag.RequestTag(ForgeManagers.Instance.TagsManager, "state.fallen");
	}

	/// <inheritdoc/>
	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		if (_entity is null || _body is null || _input is null || _entity.IsMirror)
		{
			return;
		}

		// One whose health ran out is freed by its death graph, with a burst every screen shows.
		if (_entity.Tags.AllTags.HasTag(_fallen))
		{
			_body.QueueFree();
			return;
		}

		Vector2 move = Vector2.Zero;
		float nearest = float.MaxValue;

		foreach (Node node in GetTree().GetNodesInGroup("player"))
		{
			if (node is not Node3D player)
			{
				continue;
			}

			Vector3 toPlayer = player.GlobalPosition - _body.GlobalPosition;
			var flat = new Vector2(toPlayer.X, toPlayer.Z);

			if (flat.Length() < nearest)
			{
				nearest = flat.Length();
				move = nearest > StopDistance ? flat.Normalized() : Vector2.Zero;
			}
		}

		_input.Submit(default(InputFrame).WithVector(0, new NumericsVector2(move.X, move.Y)));
	}
}
