// Copyright © Gamesmiths Guild.

using System.Linq;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Tags;
using Godot;
using Godot.Collections;
using NumericsQuaternion = System.Numerics.Quaternion;
using NumericsVector2 = System.Numerics.Vector2;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Nodes;

/// <summary>
/// Walks the <see cref="CharacterBody2D"/> its parent <see cref="ForgeEntity"/> lives on, from the entity's input, in
/// the movement stage of each physics step.
/// </summary>
/// <remarks>
/// <para>It's the entity's <see cref="IForgeEntity.Mover"/>, so it moves after the input stage and before effects and
/// graphs: an ability this step's input activated has already added its tags, and a graph's Move Body 2D moves the body
/// after it.</para>
/// <para>In the floating motion mode, for top-down games, the body walks toward the move vector times the speed. In the
/// grounded motion mode, for side views, it walks on X only and falls by the project's default gravity while it isn't
/// on the floor. While the entity has any of the <see cref="BlockingTags"/>, it stops walking, still falls, and its
/// facing stays put.</para>
/// </remarks>
[Tool]
[GlobalClass]
public partial class ForgeCharacterMovement2D : Node, IEntityMover
{
	// The project's gravity rather than the body's, which the physics server only works out as it steps the body, so
	// a tick simulated again would fall by whatever gravity the latest step found.
	private readonly Vector2 _gravity =
		ProjectSettings.GetSetting("physics/2d/default_gravity_vector").AsVector2()
		* ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

	private ForgeEntity? _entity;
	private CharacterBody2D? _body;
	private string _speedAttribute = string.Empty;
	private EntityAttribute? _speed;
	private ForgeTagContainer _blockingTagsSource = new();
	private TagContainer? _blockingTags;
	private bool _grounded;
	private bool _reportedMissingSpeed;

	/// <summary>
	/// Gets or sets the attribute the speed is read from, as <c>Set.Attribute</c>. Its decimal places give its unit, so
	/// <c>30000</c> with two decimal places is 300 pixels per second.
	/// </summary>
	[Export]
	public string SpeedAttribute
	{
		get => _speedAttribute;
		set
		{
			_speedAttribute = value;
			_speed = null;
			_reportedMissingSpeed = false;
		}
	}

	/// <summary>
	/// Gets or sets how fast the body speeds up toward the move vector, in pixels per second squared.
	/// </summary>
	[Export]
	public float Acceleration { get; set; } = 4500f;

	/// <summary>
	/// Gets or sets how fast the body slows down with no move input, in pixels per second squared.
	/// </summary>
	[Export]
	public float Friction { get; set; } = 6000f;

	/// <summary>
	/// Gets or sets the tags that stop the body walking and turning while the entity has any of them.
	/// </summary>
	[Export]
	public ForgeTagContainer BlockingTags
	{
		get => _blockingTagsSource;
		set
		{
			_blockingTagsSource = value;
			_blockingTags = null;
		}
	}

	/// <summary>
	/// Gets or sets a value indicating whether the body turns to face the input's aim.
	/// </summary>
	[Export]
	public bool FaceAim { get; set; }

	/// <summary>
	/// Gets or sets the input action that moves the body toward -X.
	/// </summary>
	[ExportGroup("Move Actions")]
	[Export]
	public string MoveLeft { get; set; } = "move_left";

	/// <summary>
	/// Gets or sets the input action that moves the body toward +X.
	/// </summary>
	[Export]
	public string MoveRight { get; set; } = "move_right";

	/// <summary>
	/// Gets or sets the input action that moves the body toward -Y, in the floating motion mode.
	/// </summary>
	[Export]
	public string MoveUp { get; set; } = "move_up";

	/// <summary>
	/// Gets or sets the input action that moves the body toward +Y, in the floating motion mode.
	/// </summary>
	[Export]
	public string MoveDown { get; set; } = "move_down";

	/// <inheritdoc/>
	public override void _EnterTree()
	{
		base._EnterTree();

		if (Engine.IsEditorHint())
		{
			return;
		}

		_entity = GetParent() as ForgeEntity;

		if (_entity is null)
		{
			GD.PushWarning($"Forge: {Name} moves the body of its parent ForgeEntity, and its parent is not one.");
			return;
		}

		if (!ForgeEntityBridge.TryGetSpatialNode2D(_entity, out Node2D? node) || node is not CharacterBody2D body)
		{
			GD.PushWarning($"Forge: {Name} moves a CharacterBody2D, and its ForgeEntity doesn't live on one.");
			return;
		}

		_body = body;
		_speed = null;
		_reportedMissingSpeed = false;
		_grounded = body.IsOnFloor();
		_entity.Mover = this;
	}

	/// <inheritdoc/>
	public override void _ExitTree()
	{
		base._ExitTree();

		if (_entity?.Mover == this)
		{
			_entity.Mover = null;
		}

		_entity = null;
		_body = null;
	}

	/// <inheritdoc/>
	public void Move(double fixedDeltaTime)
	{
		if (_entity is null || _body is null)
		{
			return;
		}

		float delta = (float)fixedDeltaTime;
		Vector2 velocity = _body.Velocity;
		bool floating = _body.MotionMode == CharacterBody2D.MotionModeEnum.Floating;

		if (!floating && !_grounded)
		{
			velocity += _gravity * delta;
		}

		_blockingTags ??= BlockingTags.GetTagContainer();

		if (_entity.Tags.AllTags.HasAny(_blockingTags))
		{
			velocity = floating ? Vector2.Zero : new Vector2(0f, velocity.Y);
		}
		else
		{
			float speed = ReadSpeed(_entity);

			if (floating)
			{
				NumericsVector2 move = _entity.Input?.GetVector(MoveLeft, MoveRight, MoveUp, MoveDown) ?? default;
				Vector2 target = new Vector2(move.X, move.Y) * speed;
				velocity = velocity.MoveToward(target, (target.IsZeroApprox() ? Friction : Acceleration) * delta);
			}
			else
			{
				// The walk axis alone, since a move vector cuts a diagonal to a length of 1 and holding up would slow
				// it.
				float target = (_entity.Input?.GetAxis(MoveLeft, MoveRight) ?? 0f) * speed;
				velocity.X = Mathf.MoveToward(
					velocity.X,
					target,
					(Mathf.IsZeroApprox(target) ? Friction : Acceleration) * delta);
			}

			if (FaceAim && _entity.Input?.Current is { HasAim: true } frame
				&& !new Vector2(frame.AimDirection.X, frame.AimDirection.Y).IsZeroApprox())
			{
				_body.GlobalRotation = Mathf.Atan2(frame.AimDirection.Y, frame.AimDirection.X);
			}
		}

		_body.Velocity = velocity;
		_body.MoveAndSlide();
		_grounded = _body.IsOnFloor();
	}

	/// <inheritdoc/>
	public BodyState CaptureState()
	{
		if (_body is null)
		{
			return default;
		}

		Vector2 position = _body.GlobalPosition;
		Vector2 velocity = _body.Velocity;

		return new BodyState(
			new NumericsVector3(position.X, position.Y, 0f),
			NumericsQuaternion.CreateFromAxisAngle(NumericsVector3.UnitZ, _body.GlobalRotation),
			new NumericsVector3(velocity.X, velocity.Y, 0f),
			_grounded);
	}

	/// <inheritdoc/>
	public void RestoreState(in BodyState state)
	{
		// A body already where the restore puts it, as each tick of a replay ends with, keeps the contacts its last
		// slide found, which are the ones its next slide starts from.
		if (_body is null || CaptureState() == state)
		{
			return;
		}

		var position = new Vector2(state.Position.X, state.Position.Y);
		float floorSnapLength = _body.FloorSnapLength;

		// The body's floor, wall and ceiling contacts only update as it slides, and its next slide starts from them, so
		// it slides in place to find the restored place's contacts. It doesn't snap, which could pull it down to a
		// floor it only reached later, and any nudge out of a contact is undone.
		_body.GlobalPosition = position;
		_body.GlobalRotation = 2f * Mathf.Atan2(state.Rotation.Z, state.Rotation.W);
		_body.Velocity = Vector2.Zero;
		_body.FloorSnapLength = 0f;
		_body.MoveAndSlide();
		_body.FloorSnapLength = floorSnapLength;
		_body.GlobalPosition = position;
		_body.Velocity = new Vector2(state.Velocity.X, state.Velocity.Y);
		_grounded = state.Grounded;
	}

#if TOOLS
	/// <inheritdoc/>
	public override void _ValidateProperty(Dictionary property)
	{
		StringName name = property["name"].AsStringName();

		// Suggested rather than enforced, so a name typed before its action is added to the settings is kept.
		if (name == PropertyName.MoveLeft || name == PropertyName.MoveRight || name == PropertyName.MoveUp
			|| name == PropertyName.MoveDown)
		{
			InputActionSet actions = ForgeSettings.GetInputActionSet();
			property["hint"] = (int)PropertyHint.EnumSuggestion;
			property["hint_string"] = string.Join(
				',',
				actions.Actions
					.Concat(actions.Vectors
						.SelectMany(x => new[] { x.NegativeX, x.PositiveX, x.NegativeY, x.PositiveY }))
					.Distinct());
		}
	}
#endif

	private float ReadSpeed(ForgeEntity entity)
	{
		if (_speed is null && !entity.Attributes.TryGetAttribute(SpeedAttribute, out _speed))
		{
			if (!_reportedMissingSpeed)
			{
				_reportedMissingSpeed = true;
				GD.PushWarning(
					$"Forge: {Name} reads its speed from [{SpeedAttribute}], and its entity has no such attribute. " +
					"The body doesn't walk.");
			}

			return 0f;
		}

		return _speed.CurrentValue / (float)_speed.DisplayScale;
	}
}
