// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Effects.Components;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Resources.Components;

[Tool]
[GlobalClass]
#pragma warning disable CA1716 // Identifiers should not match keywords
public partial class RaiseEvent : ForgeEffectComponent
#pragma warning restore CA1716 // Identifiers should not match keywords
{
	private bool _hasMagnitude;

	[Export]
	public ForgeTagContainer? EventTags { get; set; }

	[Export]
	public EffectEventTrigger Triggers { get; set; } = EffectEventTrigger.Applied;

	// One toggle per entity rather than an array of enums: the set is fixed and small, and the inspector reads better
	// as three checkboxes than as a list nothing stops you from filling with duplicates.
	[ExportGroup("Raise On")]
	[Export]
	public bool RaiseOnTarget { get; set; } = true;

	[Export]
	public bool RaiseOnSource { get; set; }

	[Export]
	public bool RaiseOnOwner { get; set; }

	[ExportGroup("Magnitude")]
	[Export]
	public bool HasMagnitude
	{
		get => _hasMagnitude;

		set
		{
			_hasMagnitude = value;
			NotifyPropertyListChanged();
		}
	}

	[Export]
	public ForgeModifierMagnitude? Magnitude { get; set; }

	public override IEffectComponent GetComponent()
	{
		EventTags ??= new();

		List<EffectApplicationTarget> raiseOn = [];

		if (RaiseOnTarget)
		{
			raiseOn.Add(EffectApplicationTarget.Target);
		}

		if (RaiseOnSource)
		{
			raiseOn.Add(EffectApplicationTarget.Source);
		}

		if (RaiseOnOwner)
		{
			raiseOn.Add(EffectApplicationTarget.Owner);
		}

		if (raiseOn.Count == 0)
		{
			GD.PushError(
				$"{nameof(RaiseEvent)}: no entity selected to raise on; falling back to the target.");
		}

		return new RaiseEventEffectComponent(
			EventTags.GetTagContainer(),
			Triggers,
			HasMagnitude && Magnitude is not null ? Magnitude.GetModifier() : null,
			[.. raiseOn]);
	}

#if TOOLS
	public override void _ValidateProperty(Dictionary property)
	{
		if (property["name"].AsStringName() == PropertyName.Magnitude && !HasMagnitude)
		{
			property["usage"] = (int)PropertyUsageFlags.NoEditor;
		}
	}
#endif
}
