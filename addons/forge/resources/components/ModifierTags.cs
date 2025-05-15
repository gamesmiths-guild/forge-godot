// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.GameplayEffects.Components;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Components;

[Tool]
[GlobalClass]
public partial class ModifierTags : EffectComponent
{
	[Export]
	public TagContainer? TagsToAdd { get; set; }

	public override IGameplayEffectComponent GetComponent()
	{
		TagsToAdd ??= new();

		return new ModifierTagsEffectComponent(TagsToAdd.GetTagContainer());
	}
}
