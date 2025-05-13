// Copyright © Gamesmiths Guild.

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
	public TagContainer? ApplicationRequiredTags { get; set; } = new();

	[Export]
	public TagContainer? ApplicationIgnoredTags { get; set; }

	[Export]
	public QueryExpression? ApplicationTagQuery { get; set; }

	[ExportGroup("Removal Requirements")]
	[Export]
	public TagContainer? RemovalRequiredTags { get; set; }

	[Export]
	public TagContainer? RemovalIgnoredTags { get; set; }

	[Export]
	public QueryExpression? RemovalTagQuery { get; set; }

	[ExportGroup("Ongoing Requirements")]
	[Export]
	public TagContainer? OngoingRequiredTags { get; set; }

	[Export]
	public TagContainer? OngoingIgnoredTags { get; set; }

	[Export]
	public QueryExpression? OngoingTagQuery { get; set; }

	public override IGameplayEffectComponent GetComponent()
	{
		ApplicationRequiredTags ??= new();
		ApplicationIgnoredTags ??= new();
		RemovalRequiredTags ??= new();
		RemovalIgnoredTags ??= new();
		OngoingRequiredTags ??= new();
		OngoingIgnoredTags ??= new();

		var applicationQuery = new GameplayTagQuery();
		if (ApplicationTagQuery is not null)
		{
			applicationQuery.Build(ApplicationTagQuery.GetQueryExpression());
		}

		var removalQuery = new GameplayTagQuery();
		if (RemovalTagQuery is not null)
		{
			removalQuery.Build(RemovalTagQuery.GetQueryExpression());
		}

		var ongoingQuery = new GameplayTagQuery();
		if (OngoingTagQuery is not null)
		{
			ongoingQuery.Build(OngoingTagQuery.GetQueryExpression());
		}

		return new TargetTagRequirementsEffectComponent(
		new GameplayTagRequirements(
			ApplicationRequiredTags.GetTagContainer(),
			ApplicationIgnoredTags.GetTagContainer(),
			applicationQuery),
		new GameplayTagRequirements(
			RemovalRequiredTags.GetTagContainer(),
			RemovalIgnoredTags.GetTagContainer(),
			removalQuery),
		new GameplayTagRequirements(
			OngoingRequiredTags.GetTagContainer(),
			OngoingIgnoredTags.GetTagContainer(),
			ongoingQuery));
	}
}
