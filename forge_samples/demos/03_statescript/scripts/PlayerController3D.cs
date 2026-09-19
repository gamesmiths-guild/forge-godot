// Copyright © Gamesmiths Guild.

using System.Linq;
using System.Text;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Providers;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Godot.Resources.Abilities;
using Gamesmiths.Forge.Tags;
using Godot;

namespace Gamesmiths.Forge.Example;

/// <summary>
/// The whole of the player's C#. Movement, aim and the HUD mirror: every ability is a Statescript graph, and this never
/// decides what one does - it samples the aim once and hands it over as activation data. The body faces the cursor
/// whenever it may move, so a graph that spawns or moves along the caster's facing is already aimed and a dash keeps
/// the heading it started with.
/// </summary>
public partial class PlayerController3D : CharacterBody3D
{
	// MovementAttributes.Speed stores two decimal places, so 500 reads as 5.00 units per second.
	private const float SpeedScale = 100f;

	private const float Acceleration = 45f;

	private const float Friction = 60f;

	// The aim ray stops on the world and on enemies, never on the player's own body under the cursor.
	private const uint AimMask = (1u << 0) | (1u << 2);

	private readonly float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

	private SkillSlot[] _skillSlots = [];

	private EntityAttribute? _speed;

	private TagContainer? _entityTags;

	private Tag _movementBlockTag;

	[Export]
	public ForgeAbilityData? ProjectileAbility { get; set; }

	[Export]
	public ForgeAbilityData? DashAbility { get; set; }

	[Export]
	public ForgeAbilityData? ShieldAbility { get; set; }

	[Export]
	public ForgeAbilityData? ThornsAbility { get; set; }

	[Export]
	public ActionBarView? Skill1View { get; set; }

	[Export]
	public ActionBarView? Skill2View { get; set; }

	[Export]
	public ActionBarView? Skill3View { get; set; }

	[Export]
	public ActionBarView? Skill4View { get; set; }

	[Export]
	public Label? TagsView { get; set; }

	public override void _Ready()
	{
		base._Ready();

		ForgeEntity forgeEntity = GetNode<ForgeEntity>("%Forge Entity");

		_entityTags = forgeEntity.Tags.AllTags;
		_movementBlockTag = Tag.RequestTag(ForgeManagers.Instance.TagsManager, "movement.block");
		_speed = forgeEntity.Attributes["MovementAttributes.Speed"];

		// Thorns has no input: it is triggered by being hit.
		(ForgeAbilityData? Data, ActionBarView? View, string? Action)[] configs =
		[
			(ProjectileAbility, Skill1View, "skill_1"),
			(DashAbility, Skill2View, "skill_2"),
			(ShieldAbility, Skill3View, "skill_3"),
			(ThornsAbility, Skill4View, null),
		];

		_skillSlots = [.. configs
			.Where(x => x.Data is not null)
			.Select(x => SkillSlot.Create(forgeEntity, x.Data!, x.View, x.Action))];
	}

	// Abilities fire from unhandled input so a focused Control consumes the event first. Aim is where the mouse
	// points on the ground, sampled once here. The facing is refreshed first so that a graph spawning along it on
	// this very press is not a physics tick behind the cursor.
	public override void _UnhandledInput(InputEvent @event)
	{
		base._UnhandledInput(@event);

		foreach (SkillSlot slot in _skillSlots)
		{
			if (slot.InputAction is null || !@event.IsActionPressed(slot.InputAction))
			{
				continue;
			}

			FaceCursor();
			slot.Handle.TryActivate(AimActivationData.FromMouseGround(this, AimMask), out AbilityActivationFailures _);
			GetViewport().SetInputAsHandled();
			return;
		}
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		foreach (SkillSlot slot in _skillSlots)
		{
			slot.UpdateView();
		}

		UpdateTagsView();
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		float dt = (float)delta;
		Vector3 velocity = Velocity;

		if (!IsOnFloor())
		{
			velocity.Y -= _gravity * dt;
		}

		// While movement is blocked the controller contributes gravity and nothing else: the dash's Move Body 3D sweeps
		// the body itself, and a walking velocity carried into the roll would add to it.
		if (_entityTags!.HasTag(_movementBlockTag))
		{
			velocity.X = 0f;
			velocity.Z = 0f;
		}
		else
		{
			Vector2 input = Input.GetVector("move_left", "move_right", "move_up", "move_down");
			Vector3 target = new Vector3(input.X, 0f, input.Y) * (_speed!.CurrentValue / SpeedScale);

			float rate = target == Vector3.Zero ? Friction : Acceleration;
			Vector3 horizontal = new Vector3(velocity.X, 0f, velocity.Z).MoveToward(target, rate * dt);
			velocity.X = horizontal.X;
			velocity.Z = horizontal.Z;
		}

		Velocity = velocity;
		MoveAndSlide();
		FaceCursor();
	}

	// The same sample the abilities are activated with, so what the player faces is exactly what a graph reading
	// Entity Rotation 3D fires along. Its direction is already flattened, and falls back to the current facing when
	// the cursor is on the player. The facing is locked while movement is: a dash points where it was aimed until
	// it ends, whatever the cursor does meanwhile.
	private void FaceCursor()
	{
		if (_entityTags!.HasTag(_movementBlockTag))
		{
			return;
		}

		Vector3 direction = AimActivationData.FromMouseGround(this, AimMask).Direction;
		Rotation = new Vector3(0f, Mathf.Atan2(-direction.X, -direction.Z), 0f);
	}

	private void UpdateTagsView()
	{
		if (TagsView is null)
		{
			return;
		}

		var tagsText = new StringBuilder();

		foreach (Tag tag in _entityTags!)
		{
			tagsText.Append(tag.ToString()).Append('\n');
		}

		TagsView.Text = tagsText.ToString();
	}

	// The same view mirror Character3D keeps in the Real-Time 3D demo, so the two HUDs read identically.
	private sealed class SkillSlot(
		AbilityHandle handle,
		ActionBarView? view,
		string? inputAction,
		Tag? cooldownTag,
		float totalCooldownTime)
	{
		public AbilityHandle Handle { get; } = handle;

		public string? InputAction { get; } = inputAction;

		public static SkillSlot Create(
			ForgeEntity forgeEntity, ForgeAbilityData data, ActionBarView? view, string? inputAction)
		{
			forgeEntity.Abilities.TryGetAbility(data.GetAbilityData(), out AbilityHandle? handle, forgeEntity);

			Tag? cooldownTag = null;
			float totalCooldownTime = 0f;
			CooldownData[]? cooldownData = handle!.GetCooldownData();

			if (cooldownData is { Length: > 0 })
			{
				cooldownTag = cooldownData[0].CooldownTags.First();
				totalCooldownTime = cooldownData[0].TotalTime;
			}

			return new SkillSlot(handle, view, inputAction, cooldownTag, totalCooldownTime);
		}

		public void UpdateView()
		{
			if (view is null)
			{
				return;
			}

			if (cooldownTag is not null)
			{
				view.UpdateCooldown(Handle.GetRemainingCooldownTime(cooldownTag.Value), totalCooldownTime);
			}

			CostData[]? costData = Handle.GetCostData();

			if (costData is { Length: > 0 })
			{
				view.UpdateCost($"{costData[0].Cost}");
			}

			view.UpdateActive(Handle.IsActive);
		}
	}
}
