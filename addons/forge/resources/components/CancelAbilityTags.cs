// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects.Components;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Components;

[Tool]
[GlobalClass]
public partial class CancelAbilityTags : ForgeEffectComponent
{
	[Export]
	public ForgeTagContainer? WithTags { get; set; }

	[Export]
	public ForgeTagContainer? WithoutTags { get; set; }

	[Export]
	public CancelAbilityTagsPolicy Policy { get; set; } = CancelAbilityTagsPolicy.OnApplication;

	public override IEffectComponent GetComponent()
	{
		WithTags ??= new();
		WithoutTags ??= new();

		return new CancelAbilityTagsEffectComponent(
			WithTags.GetTagContainer(),
			WithoutTags.GetTagContainer(),
			Policy);
	}
}
