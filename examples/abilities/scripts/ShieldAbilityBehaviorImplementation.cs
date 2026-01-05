// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Example;

public sealed class ShieldAbilityBehaviorImplementation : IAbilityBehavior
{
	private EventSubscriptionToken? _notEnoughManaSubscriptionToken;

	public void OnStarted(AbilityBehaviorContext context)
	{
		if (context.Owner is not ForgeEntity ownerNode)
		{
			return;
		}

		context.AbilityHandle.CommitAbility();

		_notEnoughManaSubscriptionToken = ownerNode.Events.Subscribe(
			Tag.RequestTag(ForgeManagers.Instance.TagsManager, "event.shield.not_enough_mana"),
			_ =>
			{
				context.InstanceHandle.End();
			});

		ForgeManagers.Instance.CuesManager.ApplyCue(
			Tag.RequestTag(ForgeManagers.Instance.TagsManager, "cue.vfx.shield"),
			ownerNode,
			null);
	}

	public void OnEnded(AbilityBehaviorContext context)
	{
		if (context.Owner is not ForgeEntity ownerNode)
		{
			return;
		}

		ForgeManagers.Instance.CuesManager.RemoveCue(
			Tag.RequestTag(ForgeManagers.Instance.TagsManager, "cue.vfx.shield"), ownerNode, false);

		ownerNode.Events.Unsubscribe(_notEnoughManaSubscriptionToken!.Value);
	}
}
