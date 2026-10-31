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
/// Walks the <see cref="CharacterBody3D"/> its parent <see cref="ForgeEntity"/> lives on, from the entity's input, in
/// the movement stage of each physics step.
/// </summary>
/// <remarks>
/// <para>It's the entity's <see cref="IForgeEntity.Mover"/>, so it moves after the input stage and before effects and
/// graphs: an ability this step's input activated has already added its tags, and a graph's Move Body 3D moves the body
/// after it.</para>
/// <para>The body walks on X and Z toward the move vector times the speed, and falls by the project's default gravity
/// while it isn't on the floor in the grounded motion mode. While the entity has any of the
/// <see cref="BlockingTags"/>, it only falls, and its facing stays put.</para>
/// </remarks>
[Tool]
[GlobalClass]
public partial class ForgeCharacterMovement3D : Node, IEntityMover
{
	// The project's gravity rather than the body's, which the physics server only works out as it steps the body, so
	// a tick simulated again would fall by whatever gravity the latest step found.
	private readonly Vector3 _gravity =
		ProjectSettings.GetSetting("physics/3d/default_gravity_vector").AsVector3()
		* ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

	private ForgeEntity? _entity;
	private CharacterBody3D? _body;
	private string _speedAttribute = string.Empty;
	private EntityAttribute? _speed;
	private ForgeTagContainer _blockingTagsSource = new();
	private TagContainer? _blockingTags;
	private bool _grounded;
	private bool _reportedMissingSpeed;

	/// <summary>
	/// Gets or sets the attribute the speed is read from, as <c>Set.Attribute</c>. Its decimal places give its unit, so
	/// <c>500</c> with two decimal places is 5 units per second.
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
	/// Gets or sets how fast the body speeds up toward the move vector, in units per second squared.
	/// </summary>
	[Export]
	public float Acceleration { get; set; } = 45f;

	/// <summary>
	/// Gets or sets how fast the body slows down with no move input, in units per second squared.
	/// </summary>
	[Export]
	public float Friction { get; set; } = 60f;

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
	/// Gets or sets the input action that moves the body toward -Z.
	/// </summary>
	[Export]
	public string MoveForward { get; set; } = "move_up";

	/// <summary>
	/// Gets or sets the input action that moves the body toward +Z.
	/// </summary>
	[Export]
	public string MoveBack { get; set; } = "move_down";

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

		if (!ForgeEntityBridge.TryGetSpatialNode3D(_entity, out Node3D? node) || node is not CharacterBody3D body)
		{
			GD.PushWarning($"Forge: {Name} moves a CharacterBody3D, and its ForgeEntity doesn't live on one.");
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
		Vector3 velocity = _body.Velocity;

		if (_body.MotionMode == CharacterBody3D.MotionModeEnum.Grounded && !_grounded)
		{
			velocity += _gravity * delta;
		}

		_blockingTags ??= BlockingTags.GetTagContainer();

		if (_entity.Tags.AllTags.HasAny(_blockingTags))
		{
			velocity.X = 0f;
			velocity.Z = 0f;
		}
		else
		{
			NumericsVector2 move = _entity.Input?.GetVector(MoveLeft, MoveRight, MoveForward, MoveBack) ?? default;
			Vector3 target = new Vector3(move.X, 0f, move.Y) * ReadSpeed(_entity);
			float rate = target.IsZeroApprox() ? Friction : Acceleration;
			Vector3 horizontal = new Vector3(velocity.X, 0f, velocity.Z).MoveToward(target, rate * delta);
			velocity.X = horizontal.X;
			velocity.Z = horizontal.Z;

			if (FaceAim && _entity.Input?.Current is { HasAim: true } frame
				&& !new Vector2(frame.AimDirection.X, frame.AimDirection.Z).IsZeroApprox())
			{
				_body.GlobalRotation = new Vector3(0f, Mathf.Atan2(-frame.AimDirection.X, -frame.AimDirection.Z), 0f);
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

		Transform3D transform = _body.GlobalTransform;
		Quaternion rotation = transform.Basis.GetRotationQuaternion();

		return new BodyState(
			ToNumerics(transform.Origin),
			new NumericsQuaternion(rotation.X, rotation.Y, rotation.Z, rotation.W),
			ToNumerics(_body.Velocity),
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

		var rotation = new Quaternion(state.Rotation.X, state.Rotation.Y, state.Rotation.Z, state.Rotation.W);
		var transform = new Transform3D(
			new Basis(rotation) * Basis.FromScale(_body.GlobalBasis.Scale),
			ToGodot(state.Position));
		float floorSnapLength = _body.FloorSnapLength;

		// The body's floor, wall and ceiling contacts only update as it slides, and its next slide starts from them, so
		// it slides in place to find the restored place's contacts. It doesn't snap, which could pull it down to a
		// floor it only reached later, and any nudge out of a contact is undone.
		_body.GlobalTransform = transform;
		_body.Velocity = Vector3.Zero;
		_body.FloorSnapLength = 0f;
		_body.MoveAndSlide();
		_body.FloorSnapLength = floorSnapLength;
		_body.GlobalTransform = transform;
		_body.Velocity = ToGodot(state.Velocity);
		_grounded = state.Grounded;
	}

#if TOOLS
	/// <inheritdoc/>
	public override void _ValidateProperty(Dictionary property)
	{
		StringName name = property["name"].AsStringName();

		// Suggested rather than enforced, so a name typed before its action is added to the settings is kept.
		if (name == PropertyName.MoveLeft || name == PropertyName.MoveRight || name == PropertyName.MoveForward
			|| name == PropertyName.MoveBack)
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

	private static NumericsVector3 ToNumerics(Vector3 value)
	{
		return new NumericsVector3(value.X, value.Y, value.Z);
	}

	private static Vector3 ToGodot(NumericsVector3 value)
	{
		return new Vector3(value.X, value.Y, value.Z);
	}

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
