// Copyright © 2025 Gamesmiths Guild.

using Gamesmiths.Forge.GameplayTags;
using Gamesmiths.Forge.GameplayTags.Godot;
using Godot;

namespace Gamesmiths.Forge.GameplayEffects.Components.Godot;

[Tool]
[GlobalClass]
public partial class TargetTagRequirements : EffectComponent
{
	[ExportGroup("Application Requirements")]
	[Export]
	public TagContainer ApplicationRequiredTags { get; set; }

	[Export]
	public TagContainer ApplicationIgnoredTags { get; set; }

	[ExportGroup("Removal Requirements")]
	[Export]
	public TagContainer RemovalRequiredTags { get; set; }

	[Export]
	public TagContainer RemovalIgnoredTags { get; set; }

	[ExportGroup("Ongoing Requirements")]
	[Export]
	public TagContainer OngoingRequiredTags { get; set; }

	[Export]
	public TagContainer OngoingIgnoredTags { get; set; }

	public override IGameplayEffectComponent GetComponent()
	{
		return new TargetTagRequirementsEffectComponent(
			new GameplayTagRequirements(
				ApplicationRequiredTags.GetTagContainer(),
				ApplicationIgnoredTags.GetTagContainer(),
				new GameplayTagQuery()),
			new GameplayTagRequirements(
				RemovalRequiredTags.GetTagContainer(),
				RemovalIgnoredTags.GetTagContainer(),
				new GameplayTagQuery()),
			new GameplayTagRequirements(
				OngoingRequiredTags.GetTagContainer(),
				OngoingIgnoredTags.GetTagContainer(),
				new GameplayTagQuery()));
	}
}
