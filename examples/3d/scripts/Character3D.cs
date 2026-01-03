// Copyright © Gamesmiths Guild.

using System.Linq;
using System.Text;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Events;
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
	private AbilityHandle? _projectileAbilityHandle;
	private Tag _cooldownTag;
	private float _totalCooldownTime;
	private TagContainer? _entityTags;

	[Export]
	public ForgeAbilityData? ProjectileAbilityData { get; set; }

	[Export]
	public CooldownView? CooldownView { get; set; }

	public override void _Ready()
	{
		base._Ready();

		ForgeEntity forgeEntity = GetNode<ForgeEntity>("%Forge Entity");

		forgeEntity.Abilities.TryGetAbility(
			ProjectileAbilityData!.GetAbilityData(),
			out _projectileAbilityHandle,
			forgeEntity);

		_entityTags = forgeEntity.Tags.CombinedTags;

		CooldownData[]? cooldownData = _projectileAbilityHandle!.GetCooldownData();
		_cooldownTag = cooldownData![0].CooldownTags.First();
		_totalCooldownTime = cooldownData[0].TotalTime;

		forgeEntity.Attributes["CharacterAttributes.Health"].OnValueChanged += (_, change) =>
		{
			if (change <= 0)
			{
				GD.Print("Damage event raised");

				forgeEntity.Events.Raise(new EventData
				{
					EventTags =
						Tag.RequestTag(ForgeManagers.Instance.TagsManager, "event.damage").GetSingleTagContainer()!,
					Source = forgeEntity,
					Target = forgeEntity,
					EventMagnitude = change,
				});
			}
		};
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		if (Input.IsActionJustPressed("skill_1"))
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
				return;
			}

			var mouseWorldPos = (Vector3)result["position"];

			Vector3 sourcePos = GlobalTransform.Origin;
			Vector3 direction = (mouseWorldPos - sourcePos).Normalized();

			_projectileAbilityHandle!.Activate(
				new TargetData { Direction = direction },
				out AbilityActivationFailures activationResult);

			GD.Print($"Ability activation result: {activationResult}");
		}

		if (CooldownView is not null && _projectileAbilityHandle is not null)
		{
			var cooldownRemaining = _projectileAbilityHandle.GetRemainingCooldownTime(_cooldownTag);
			CooldownView.UpdateCooldown(cooldownRemaining, _totalCooldownTime);

			var costText = _projectileAbilityHandle.GetCostData()![0].Cost;
			CooldownView.UpdateCost($"{costText}");

			var tagsText = new StringBuilder();
			foreach (Tag tag in _entityTags!)
			{
				tagsText.Append(tag.ToString() + "\n");
			}

			CooldownView.UpdateTags(tagsText.ToString());
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		Vector2 inputDirection = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		Vector3 direction = (Transform.Basis * new Vector3(inputDirection.X, 0, inputDirection.Y)).Normalized();

		Velocity = direction * Speed;

		MoveAndSlide();
	}

	public record struct TargetData(Vector3 Direction);
}
