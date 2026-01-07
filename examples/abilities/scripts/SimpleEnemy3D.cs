// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Godot.Resources.Abilities;
using Gamesmiths.Forge.Tags;
using Godot;

namespace Gamesmiths.Forge.Example;

public partial class SimpleEnemy3D : CharacterBody3D
{
	private const float Speed = 3f;

	private TagContainer? _entityTags;

	private Node3D? _player;

	private ForgeEntity? _forgePlayer;

	private AbilityHandle? _abilityHandle;

	[Export]
	public ForgeAbilityData? EnemyAttackAbilityData { get; set; }

	[Export]
	public required Area3D AttackRange { get; set; }

	public override void _Ready()
	{
		base._Ready();

		ForgeEntity forgeEntity = GetNode<ForgeEntity>("%Forge Entity");
		_player = GetTree().CurrentScene?.GetNodeOrNull<Node3D>("Player");
		_forgePlayer = _player?.GetNodeOrNull<ForgeEntity>("%Forge Entity");

		forgeEntity.Abilities.TryGetAbility(EnemyAttackAbilityData!.GetAbilityData(), out _abilityHandle, forgeEntity);

		forgeEntity.Attributes["CharacterAttributes.Health"].OnValueChanged += (attribute, _) =>
		{
			if (attribute.CurrentValue <= 0)
			{
				QueueFree();
			}
		};

		_entityTags = forgeEntity.Tags.CombinedTags;
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		foreach (Node3D? body in AttackRange.GetOverlappingBodies())
		{
			if (body != _player)
			{
				continue;
			}

			_abilityHandle!.Activate(out AbilityActivationFailures _, _forgePlayer);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		if (_entityTags!.HasTag(Tag.RequestTag(ForgeManagers.Instance.TagsManager, "movement.block")))
		{
			return;
		}

		// world-space vector from this to the player
		Vector3 toPlayer = _player!.GlobalTransform.Origin - GlobalTransform.Origin;
		if (toPlayer == Vector3.Zero)
		{
			return;
		}

		// Optional: keep movement on the XZ plane
		toPlayer.Y = 0;

		Vector3 direction = toPlayer.Normalized();

		Velocity = direction * Speed;

		MoveAndSlide();
	}
}
