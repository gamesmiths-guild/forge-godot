// Copyright © Gamesmiths Guild.

using System.Diagnostics;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Tags;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Resources.Components;

[Tool]
[GlobalClass]
public partial class ForgeConditionalEffect : Resource
{
	private bool _removeAllStacks = true;
	private ConditionalEffectRemovalPolicy _removalPolicy;

	[Export]
	public ForgeEffectData? EffectData { get; set; }

	[Export]
	public EffectApplicationTarget ApplicationTarget { get; set; }

	[ExportGroup("Source Requirements")]
	[Export]
	public ForgeTagContainer? SourceRequiredTags { get; set; }

	[Export]
	public ForgeTagContainer? SourceIgnoredTags { get; set; }

	[Export]
	public ForgeQueryExpression? SourceTagQuery { get; set; }

	[ExportGroup("Removal")]
	[Export]
	public ConditionalEffectRemovalPolicy RemovalPolicy
	{
		get => _removalPolicy;

		set
		{
			_removalPolicy = value;
			NotifyPropertyListChanged();
		}
	}

	[Export]
	public bool RemoveAllStacks
	{
		get => _removeAllStacks;

		set
		{
			_removeAllStacks = value;
			NotifyPropertyListChanged();
		}
	}

	[Export(PropertyHint.Range, "1,10,1,or_greater")]
	public int StacksToRemove { get; set; } = 1;

	public ConditionalEffect GetConditionalEffect()
	{
		Debug.Assert(EffectData is not null, $"{nameof(EffectData)} reference is missing.");

		SourceRequiredTags ??= new();
		SourceIgnoredTags ??= new();

		var sourceQuery = new TagQuery();
		if (SourceTagQuery is not null)
		{
			sourceQuery.Build(SourceTagQuery.GetQueryExpression());
		}

		return new ConditionalEffect(
			EffectData.GetEffectData(),
			new TagRequirements(
				SourceRequiredTags.GetTagContainer(),
				SourceIgnoredTags.GetTagContainer(),
				sourceQuery),
			RemovalPolicy,
			RemoveAllStacks ? -1 : Mathf.Max(1, StacksToRemove),
			ApplicationTarget);
	}

#if TOOLS
	public override void _ValidateProperty(Dictionary property)
	{
		StringName propertyName = property["name"].AsStringName();

		if (RemovalPolicy != ConditionalEffectRemovalPolicy.RemoveOnEnd
			&& (propertyName == PropertyName.RemoveAllStacks || propertyName == PropertyName.StacksToRemove))
		{
			property["usage"] = (int)PropertyUsageFlags.NoEditor;
		}

		if (propertyName == PropertyName.StacksToRemove && RemoveAllStacks)
		{
			property["usage"] = (int)PropertyUsageFlags.NoEditor;
		}
	}
#endif
}
