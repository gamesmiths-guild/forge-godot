// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Godot.Nodes;
using Godot;

namespace Gamesmiths.Forge.Example;

public sealed class ProjectileAbilityBehaviorImplementation : IAbilityBehavior<Character3D.TargetData>
{
	private readonly PackedScene _projectileScene;

	public ProjectileAbilityBehaviorImplementation(PackedScene projectileScene)
	{
		_projectileScene = projectileScene;
	}

	public void OnStarted(AbilityBehaviorContext context, Character3D.TargetData data)
	{
		if (context.Owner is not ForgeEntity ownerNode)
		{
			return;
		}

		context.AbilityHandle.CommitAbility();

		Projectile projectile = _projectileScene.Instantiate<Projectile>();
		ownerNode.GetTree().Root.AddChild(projectile);

		Node3D parentNode = ownerNode.GetParent<Node3D>();

		projectile.GlobalTransform = parentNode.GlobalTransform.Translated(data.Direction + Vector3.Up);
		projectile.Launch(data.Direction, 10f);
		context.InstanceHandle.End();
	}

	public void OnEnded(AbilityBehaviorContext context)
	{
	}
}
