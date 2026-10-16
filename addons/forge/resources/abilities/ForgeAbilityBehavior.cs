// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Tags;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Abilities;

[Tool]
[GlobalClass]
[Icon("uid://bcx7anhepqfmd")]
public abstract partial class ForgeAbilityBehavior : Resource
{
	public abstract IAbilityBehavior GetBehavior();

	/// <summary>
	/// Creates the trigger data used when the ability is triggered by an event. Override it when
	/// <see cref="GetBehavior"/> returns an <see cref="IAbilityBehavior{TData}"/> that should receive the event
	/// payload, returning <c>AbilityTriggerData.ForEvent&lt;TData&gt;(triggerTag, priority)</c>.
	/// </summary>
	/// <param name="triggerTag">The tag that triggers the ability.</param>
	/// <param name="priority">The priority of the event subscription.</param>
	/// <returns>The event trigger data.</returns>
	public virtual AbilityTriggerData CreateEventTrigger(Tag triggerTag, int priority)
	{
		return AbilityTriggerData.ForEvent(triggerTag, priority);
	}
}
