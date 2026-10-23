// Copyright © Gamesmiths Guild.

using System.Linq;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Providers;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Nodes;

/// <summary>
/// Collects the local player's input for its parent <see cref="ForgeEntity"/>: one frame each physics tick, holding the
/// project's input actions and vectors (<see cref="ForgeSettings.InputActionsSetting"/>,
/// <see cref="ForgeSettings.InputVectorsSetting"/>) and the aim.
/// </summary>
/// <remarks>
/// <para>Presses and releases are caught as input events arrive, from unhandled input, so a press the interface
/// consumes never reaches the game, and a tap shorter than a physics tick still counts. Held actions, vectors and the
/// aim are sampled when the tick begins.</para>
/// <para>The collector processes its physics step before every other node, so the frame it submits is the one the
/// entity's tick reads in the same physics step.</para>
/// </remarks>
[GlobalClass]
public partial class ForgeInputCollector : Node
{
	private ForgeEntity? _entity;
	private EntityInput? _input;
	private StringName[] _actions = [];
	private StringName[][] _vectors = [];
	private ulong _held;
	private ulong _pressed;
	private ulong _released;

	/// <summary>
	/// Gets or sets where the aim is read from.
	/// </summary>
	[Export]
	public ForgeAimSource AimSource { get; set; }

	/// <summary>
	/// Gets or sets the physics layers a 3D aim's ray hits.
	/// </summary>
	[Export(PropertyHint.Layers3DPhysics)]
	public uint AimCollisionMask { get; set; } = uint.MaxValue;

	/// <inheritdoc/>
	public override void _Ready()
	{
		base._Ready();

		if (Engine.IsEditorHint())
		{
			return;
		}

		_entity = GetParent() as ForgeEntity;

		if (_entity is null)
		{
			GD.PushWarning($"Forge: {Name} collects input for its parent ForgeEntity, and its parent is not one.");
			return;
		}

		InputActionSet actions = ForgeSettings.GetInputActionSet();
		_input = new EntityInput(actions);
		_entity.Input = _input;

		_actions = [.. actions.Actions.Select(x => new StringName(x))];
		_vectors =
		[
			.. actions.Vectors.Select(x => new StringName[]
			{
				x.NegativeX,
				x.PositiveX,
				x.NegativeY,
				x.PositiveY,
			}),
		];

		ProcessPhysicsPriority = int.MinValue;
	}

	/// <inheritdoc/>
	public override void _UnhandledInput(InputEvent @event)
	{
		base._UnhandledInput(@event);

		for (int i = 0; i < _actions.Length; i++)
		{
			if (!@event.IsAction(_actions[i]))
			{
				continue;
			}

			// Godot has already applied the event to the action's state, which counts every key bound to the action:
			// releasing one of two held keys leaves the action held.
			ulong bit = 1UL << i;
			bool held = Input.IsActionPressed(_actions[i]);

			if (held && (_held & bit) == 0)
			{
				_pressed |= bit;
			}
			else if (!held && (_held & bit) != 0)
			{
				_released |= bit;
			}

			_held = held ? _held | bit : _held & ~bit;
		}
	}

	/// <inheritdoc/>
	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		if (_input is null)
		{
			return;
		}

		ulong held = 0;

		for (int i = 0; i < _actions.Length; i++)
		{
			if (Input.IsActionPressed(_actions[i]))
			{
				held |= 1UL << i;
			}
		}

		// A change no unhandled event announced - a press the interface consumed - is taken as it stands, with no edge.
		_held = held;

		var frame = new InputFrame { Held = held, Pressed = _pressed, Released = _released };

		for (int i = 0; i < _vectors.Length; i++)
		{
			StringName[] vector = _vectors[i];
			Vector2 value = Input.GetVector(vector[0], vector[1], vector[2], vector[3]);
			frame = frame.WithVector(i, new NumericsVector2(value.X, value.Y));
		}

		_input.Submit(WithAim(frame));
		_pressed = 0;
		_released = 0;
	}

	private static NumericsVector3 ToNumerics(Vector3 value)
	{
		return new NumericsVector3(value.X, value.Y, value.Z);
	}

	private InputFrame WithAim(InputFrame frame)
	{
		switch (AimSource)
		{
			case ForgeAimSource.MouseGround3D when OwningNode() is Node3D node:
				var ground = AimActivationData.FromMouseGround(node, AimCollisionMask);
				return frame.WithAim(ToNumerics(ground.TargetPoint), ToNumerics(ground.Direction));

			case ForgeAimSource.Camera3D when GetViewport()?.GetCamera3D() is Camera3D camera:
				var look = AimActivationData.FromCamera(camera, AimCollisionMask);
				return frame.WithAim(ToNumerics(look.TargetPoint), ToNumerics(look.Direction));

			case ForgeAimSource.Mouse2D when OwningNode() is Node2D node:
				var mouse = AimActivationData2D.FromMouse(node);
				return frame.WithAim(
					new NumericsVector3(mouse.TargetPoint.X, mouse.TargetPoint.Y, 0),
					new NumericsVector3(mouse.Direction.X, mouse.Direction.Y, 0));

			default:
				return frame;
		}
	}

	private Node? OwningNode()
	{
		return ForgeEntityBridge.TryGetOwningNode(_entity, string.Empty, out Node? node) ? node : null;
	}
}
