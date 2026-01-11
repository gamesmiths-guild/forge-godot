// Copyright © Gamesmiths Guild.

using System;
using System.Linq;
using System.Text;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Godot.Resources.Abilities;
using Gamesmiths.Forge.Tags;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Example;

public partial class Character3D : CharacterBody3D
{
	private const float Speed = 5f;
	private const int FloorLayer = 1 << 0;
	private const int SkillCount = 4;

	private readonly SkillSlot[] _skillSlots = new SkillSlot[SkillCount];
	private TagContainer? _entityTags;
	private Vector3 _previousVelocity = Vector3.Forward;

	[Export]
	public ForgeAbilityData? ProjectileAbilityData { get; set; }

	[Export]
	public ForgeAbilityData? DashAbilityData { get; set; }

	[Export]
	public ForgeAbilityData? ShieldAbilityData { get; set; }

	[Export]
	public ForgeAbilityData? ThornsAbilityData { get; set; }

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
		_entityTags = forgeEntity.Tags.CombinedTags;

		InitializeSkillSlots(forgeEntity);
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		HandleSkillInputs();
		UpdateSkillViews();
		UpdateTagsView();
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		if (!_entityTags!.HasTag(Tag.RequestTag(ForgeManagers.Instance.TagsManager, "movement.block")))
		{
			Vector2 inputDirection = Input.GetVector("move_left", "move_right", "move_up", "move_down");
			Vector3 direction = (Transform.Basis * new Vector3(inputDirection.X, 0, inputDirection.Y)).Normalized();
			Velocity = direction * Speed;

			if (Velocity != Vector3.Zero)
			{
				_previousVelocity = Velocity;
			}
		}

		MoveAndSlide();
	}

	private static SkillSlot CreateSkillSlot(ForgeEntity forgeEntity, SkillConfig config)
	{
		forgeEntity.Abilities.TryGetAbility(
			config.AbilityData!.GetAbilityData(),
			out AbilityHandle? handle,
			forgeEntity);

		Tag? cooldownTag = null;
		var totalCooldownTime = 0f;

		if (config.HasCooldown)
		{
			CooldownData[]? cooldownData = handle!.GetCooldownData();
			if (cooldownData is { Length: > 0 })
			{
				cooldownTag = cooldownData[0].CooldownTags.First();
				totalCooldownTime = cooldownData[0].TotalTime;
			}
		}

		return new SkillSlot(
			handle!,
			config.View,
			config.InputAction,
			cooldownTag,
			totalCooldownTime,
			config.ActivationStrategy);
	}

	private static void ActivateToggleAbility(AbilityHandle handle)
	{
		if (!handle.IsActive)
		{
			handle.Activate(out AbilityActivationFailures activationResult);
			GD.Print($"{handle} activation result: {activationResult}");
		}
		else
		{
			handle.Cancel();
			GD.Print($"{handle} canceled.");
		}
	}

	private void InitializeSkillSlots(ForgeEntity forgeEntity)
	{
		SkillConfig[] skillConfigs =
		[
			new SkillConfig(
				ProjectileAbilityData, Skill1View, "skill_1", ActivateDirectionalAbility, HasCooldown: true),
			new SkillConfig(
				DashAbilityData, Skill2View, "skill_2", ActivateDirectionalAbility, HasCooldown: true),
			new SkillConfig(
				ShieldAbilityData, Skill3View, "skill_3", ActivateToggleAbility, HasCooldown: false),
			new SkillConfig(
				ThornsAbilityData, Skill4View, InputAction: null, ActivationStrategy: null, HasCooldown: true),
		];

		for (var i = 0; i < SkillCount; i++)
		{
			_skillSlots[i] = CreateSkillSlot(forgeEntity, skillConfigs[i]);
		}
	}

	private void HandleSkillInputs()
	{
		foreach (SkillSlot slot in _skillSlots.Where(
			x => x.CanActivateByInput && Input.IsActionJustPressed(x.InputAction!)))
		{
			slot.ActivationStrategy!(slot.Handle);
		}
	}

	private void UpdateSkillViews()
	{
		foreach (SkillSlot slot in _skillSlots)
		{
			slot.UpdateView();
		}
	}

	private void UpdateTagsView()
	{
		if (TagsView is null || _entityTags is null)
		{
			return;
		}

		var tagsText = new StringBuilder();
		foreach (Tag tag in _entityTags!)
		{
			tagsText.Append(tag.ToString() + "\n");
		}

		TagsView.Text = tagsText.ToString();
	}

	private void ActivateDirectionalAbility(AbilityHandle handle)
	{
		handle.Activate(
			new TargetData { Direction = GetMouseDirection() },
			out AbilityActivationFailures activationResult);

		GD.Print($"{handle} activation result: {activationResult}");
	}

	private Vector3 GetMouseDirection()
	{
		Camera3D camera = GetViewport().GetCamera3D();

		Vector2 mousePos = GetViewport().GetMousePosition();

		Vector3 rayOrigin = camera.ProjectRayOrigin(mousePos);
		Vector3 rayDirection = camera.ProjectRayNormal(mousePos);

		PhysicsDirectSpaceState3D spaceState = GetWorld3D().DirectSpaceState;

		var query = PhysicsRayQueryParameters3D.Create(
			rayOrigin,
			rayOrigin + (rayDirection * 1000f));

		query.CollisionMask = FloorLayer;
		query.CollideWithAreas = false;

		Dictionary? result = spaceState.IntersectRay(query);
		if (result.Count == 0)
		{
			return Vector3.Zero;
		}

		var mouseWorldPos = (Vector3)result["position"];

		Vector3 sourcePos = GlobalTransform.Origin;
		return (mouseWorldPos - sourcePos).Normalized();
	}

	private sealed class SkillSlot(
		AbilityHandle handle,
		ActionBarView? view,
		string? inputAction,
		Tag? cooldownTag,
		float totalCooldownTime,
		Action<AbilityHandle>? activationStrategy)
	{
		private readonly Tag? _cooldownTag = cooldownTag;

		private readonly float _totalCooldownTime = totalCooldownTime;

		public AbilityHandle Handle { get; } = handle;

		public string? InputAction { get; } = inputAction;

		public Action<AbilityHandle>? ActivationStrategy { get; } = activationStrategy;

		public bool CanActivateByInput => InputAction is not null && ActivationStrategy is not null;

		public void UpdateView()
		{
			if (view is null)
			{
				return;
			}

			if (_cooldownTag is not null)
			{
				var cooldownRemaining = Handle.GetRemainingCooldownTime(_cooldownTag.Value);
				view.UpdateCooldown(cooldownRemaining, _totalCooldownTime);
			}

			CostData[]? costData = Handle.GetCostData();
			if (costData is { Length: > 0 })
			{
				view.UpdateCost($"{costData[0].Cost}");
			}

			view.UpdateActive(Handle.IsActive);
		}
	}

	public record struct TargetData(Vector3 Direction);

	private readonly record struct SkillConfig(
		ForgeAbilityData? AbilityData,
		ActionBarView? View,
		string? InputAction,
		Action<AbilityHandle>? ActivationStrategy,
		bool HasCooldown);
}
