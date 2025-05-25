// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects.Components;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Components;

[Tool]
[GlobalClass]
public partial class ModifierTags : ForgeEffectComponent
{
	[Export]
	public ForgeTagContainer? TagsToAdd { get; set; }

	public override IEffectComponent GetComponent()
	{
		TagsToAdd ??= new();

		return new ModifierTagsEffectComponent(TagsToAdd.GetTagContainer());
	}
}
