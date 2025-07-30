// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Tags;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Components;

[Tool]
[GlobalClass]
public partial class TargetTagRequirements : ForgeEffectComponent
{
	[ExportGroup("Application Requirements")]
	[Export]
	public ForgeTagContainer? ApplicationRequiredTags { get; set; }

	[Export]
	public ForgeTagContainer? ApplicationIgnoredTags { get; set; }

	[Export]
	public QueryExpression? ApplicationTagQuery { get; set; }

	[ExportGroup("Removal Requirements")]
	[Export]
	public ForgeTagContainer? RemovalRequiredTags { get; set; }

	[Export]
	public ForgeTagContainer? RemovalIgnoredTags { get; set; }

	[Export]
	public QueryExpression? RemovalTagQuery { get; set; }

	[ExportGroup("Ongoing Requirements")]
	[Export]
	public ForgeTagContainer? OngoingRequiredTags { get; set; }

	[Export]
	public ForgeTagContainer? OngoingIgnoredTags { get; set; }

	[Export]
	public QueryExpression? OngoingTagQuery { get; set; }

	public override IEffectComponent GetComponent()
	{
		ApplicationRequiredTags ??= new();
		ApplicationIgnoredTags ??= new();
		RemovalRequiredTags ??= new();
		RemovalIgnoredTags ??= new();
		OngoingRequiredTags ??= new();
		OngoingIgnoredTags ??= new();

		var applicationQuery = new TagQuery();
		if (ApplicationTagQuery is not null)
		{
			applicationQuery.Build(ApplicationTagQuery.GetQueryExpression());
		}

		var removalQuery = new TagQuery();
		if (RemovalTagQuery is not null)
		{
			removalQuery.Build(RemovalTagQuery.GetQueryExpression());
		}

		var ongoingQuery = new TagQuery();
		if (OngoingTagQuery is not null)
		{
			ongoingQuery.Build(OngoingTagQuery.GetQueryExpression());
		}

		return new TargetTagRequirementsEffectComponent(
		new TagRequirements(
			ApplicationRequiredTags.GetTagContainer(),
			ApplicationIgnoredTags.GetTagContainer(),
			applicationQuery),
		new TagRequirements(
			RemovalRequiredTags.GetTagContainer(),
			RemovalIgnoredTags.GetTagContainer(),
			removalQuery),
		new TagRequirements(
			OngoingRequiredTags.GetTagContainer(),
			OngoingIgnoredTags.GetTagContainer(),
			ongoingQuery));
	}
}
