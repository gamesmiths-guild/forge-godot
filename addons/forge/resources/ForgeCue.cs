// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Cues;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Resources;

[Tool]
[GlobalClass]
[Icon("uid://din7fexs0x53h")]
public partial class ForgeCue : Resource
{
	private CueMagnitudeType _magnitudeType;
	private string? _magnitudeAttributeSet;

	[Export]
	public required ForgeTagContainer CueKeys { get; set; }

	[Export]
	public int MinValue { get; set; }

	[Export]
	public int MaxValue { get; set; }

	[Export]
	public CueMagnitudeType MagnitudeType
	{
		get => _magnitudeType;

		set
		{
			_magnitudeType = value;
			NotifyPropertyListChanged();
		}
	}

	[ExportGroup("Magnitude Attribute")]
	[Export]
	public string? MagnitudeAttributeSet
	{
		get => _magnitudeAttributeSet;

		set
		{
			_magnitudeAttributeSet = value;
			MagnitudeAttribute = null;
			NotifyPropertyListChanged();
		}
	}

	[Export]
	public string? MagnitudeAttribute { get; set; }

	public CueData GetCueData()
	{
		return new CueData(
			CueKeys.GetTagContainer(),
			MinValue,
			MaxValue,
			MagnitudeType,
			string.IsNullOrEmpty(MagnitudeAttribute) ? null : MagnitudeAttribute);
	}

#if TOOLS
	public override void _ValidateProperty(Dictionary property)
	{
		if (property["name"].AsStringName() == PropertyName.MagnitudeAttributeSet)
		{
			property["hint"] = (int)PropertyHint.Enum;
			property["hint_string"] = string.Join(",", Editor.EditorUtils.GetAttributeSetOptions());

			if (MagnitudeType == CueMagnitudeType.EffectLevel || MagnitudeType == CueMagnitudeType.StackCount)
			{
				property["usage"] = (int)(PropertyUsageFlags.Default | PropertyUsageFlags.ReadOnly);
				MagnitudeAttributeSet = null;
			}
		}

		if (property["name"].AsStringName() == PropertyName.MagnitudeAttribute)
		{
			property["hint"] = (int)PropertyHint.Enum;
			property["hint_string"] = string.Join(",", Editor.EditorUtils.GetAttributeOptions(MagnitudeAttributeSet));

			if (MagnitudeType == CueMagnitudeType.EffectLevel
				|| MagnitudeType == CueMagnitudeType.StackCount
				|| string.IsNullOrEmpty(MagnitudeAttributeSet))
			{
				property["usage"] = (int)(PropertyUsageFlags.Default | PropertyUsageFlags.ReadOnly);
			}
		}
	}
#endif
}
