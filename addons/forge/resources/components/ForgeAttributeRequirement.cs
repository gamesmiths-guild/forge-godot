// Copyright © Gamesmiths Guild.

using System.Diagnostics;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Magnitudes;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Resources.Components;

[Tool]
[GlobalClass]
public partial class ForgeAttributeRequirement : Resource
{
	private AttributeCalculationType _calculationType;
	private bool _hasMinValue;
	private bool _hasMaxValue;

	[ExportGroup("Attribute")]
	[Export]
	public string? Attribute { get; set; }

	// Godot cannot export a nullable float, so each bound is an explicit toggle plus its value.
	[ExportGroup("Bounds")]
	[Export]
	public bool HasMinValue
	{
		get => _hasMinValue;

		set
		{
			_hasMinValue = value;
			NotifyPropertyListChanged();
		}
	}

	[Export]
	public float MinValue { get; set; }

	[Export]
	public bool HasMaxValue
	{
		get => _hasMaxValue;

		set
		{
			_hasMaxValue = value;
			NotifyPropertyListChanged();
		}
	}

	[Export]
	public float MaxValue { get; set; }

	[Export]
	public AttributeThresholdType ThresholdType { get; set; }

	[ExportGroup("Value Selection")]
	[Export]
	public AttributeCalculationType CalculationType
	{
		get => _calculationType;

		set
		{
			_calculationType = value;
			NotifyPropertyListChanged();
		}
	}

	[Export]
	public int FinalChannel { get; set; }

	public AttributeRequirement GetAttributeRequirement()
	{
		Debug.Assert(Attribute is not null, $"{nameof(Attribute)} reference is missing.");

		return new AttributeRequirement(
			Attribute,
			HasMinValue ? MinValue : null,
			HasMaxValue ? MaxValue : null,
			ThresholdType,
			CalculationType,
			FinalChannel);
	}

#if TOOLS
	public override void _ValidateProperty(Dictionary property)
	{
		if (property["name"].AsStringName() == PropertyName.MinValue && !HasMinValue)
		{
			property["usage"] = (int)PropertyUsageFlags.NoEditor;
		}

		if (property["name"].AsStringName() == PropertyName.MaxValue && !HasMaxValue)
		{
			property["usage"] = (int)PropertyUsageFlags.NoEditor;
		}

		if (property["name"].AsStringName() == PropertyName.FinalChannel &&
			CalculationType != AttributeCalculationType.MagnitudeEvaluatedUpToChannel)
		{
			property["usage"] = (int)PropertyUsageFlags.NoEditor;
		}
	}
#endif
}
