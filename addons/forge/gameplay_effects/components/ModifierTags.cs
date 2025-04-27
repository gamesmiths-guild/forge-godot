// Copyright © 2025 Gamesmiths Guild.

using Gamesmiths.Forge.GameplayTags.Godot;
using Godot;

namespace Gamesmiths.Forge.GameplayEffects.Components.Godot;

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
