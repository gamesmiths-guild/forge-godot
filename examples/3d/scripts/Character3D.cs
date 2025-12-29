// Copyright © Gamesmiths Guild.

using System.Linq;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Godot.Resources.Abilities;
using Gamesmiths.Forge.Tags;
using Godot;

namespace Gamesmiths.Forge.Example;

public partial class Character3D : CharacterBody3D
{
	private const float Speed = 5f;

	private AbilityHandle? _abilityHandle;
	private Tag _cooldownTag;
	private float _totalCooldownTime;
	private TagContainer? _entityTags;

	[Export]
	public ForgeAbilityData? TestAbilityData { get; set; }

	[Export]
	public CooldownView? CooldownView { get; set; }

	public override void _Ready()
	{
		base._Ready();

		ForgeEntity forgeEntity = GetNode<ForgeEntity>("%Forge Entity");

		forgeEntity?.Abilities.TryGetAbility(TestAbilityData!.GetAbilityData(), out _abilityHandle, forgeEntity);

		_entityTags = forgeEntity.Tags.CombinedTags;

		CooldownData[]? cooldownData = _abilityHandle!.GetCooldownData();
		_cooldownTag = cooldownData![0].CooldownTags.First();
		_totalCooldownTime = cooldownData[0].TotalTime;
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		if (Input.IsActionJustPressed("skill_1"))
		{
			_abilityHandle!.Activate(out AbilityActivationFailures result);
			GD.Print($"Ability activation result: {result}");
		}

		if (CooldownView is not null && _abilityHandle is not null)
		{
			var cooldownRemaining = _abilityHandle.GetRemainingCooldownTime(_cooldownTag);
			CooldownView.UpdateCooldown(cooldownRemaining, _totalCooldownTime);

			var costText = _abilityHandle.GetCostData()![0].Cost;
			CooldownView.UpdateCost(costText.ToString());

			var tagsText = "";
			foreach (var tag in _entityTags)
			{
				tagsText += tag.ToString() + "\n";
			}
			CooldownView.UpdateTags(tagsText);
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
}
