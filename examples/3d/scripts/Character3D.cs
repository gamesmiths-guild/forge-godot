// Copyright © Gamesmiths Guild.

using System.Linq;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Godot.Resources.Abilities;
using Godot;

namespace Gamesmiths.Forge.Example;

public partial class Character3D : CharacterBody3D
{
	private const float Speed = 5f;

	private AbilityHandle? _abilityHandle;

	[Export]
	public ForgeAbilityData? TestAbilityData { get; set; }

	public override void _Ready()
	{
		base._Ready();

		ForgeEntity forgeEntity = GetNode<ForgeEntity>("%Forge Entity");

		_abilityHandle = forgeEntity?.Abilities.GrantedAbilities.FirstOrDefault();
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		if (Input.IsActionJustPressed("skill_1"))
		{
			_abilityHandle!.Activate(out AbilityActivationResult result);
			GD.Print($"Ability activation result: {result}");
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
