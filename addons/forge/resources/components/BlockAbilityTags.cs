// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects.Components;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Components;

[Tool]
[GlobalClass]
public partial class BlockAbilityTags : ForgeEffectComponent
{
	[Export]
	public ForgeTagContainer? TagsToBlock { get; set; }

	public override IEffectComponent GetComponent()
	{
		TagsToBlock ??= new();

		return new BlockAbilityTagsEffectComponent(TagsToBlock.GetTagContainer());
	}
}
