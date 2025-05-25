// Copyright © Gamesmiths Guild.

using System.Diagnostics;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Godot.Core;
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
	public string? CueKey { get; set; }

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
		Debug.Assert(!string.IsNullOrEmpty(CueKey), $"{nameof(CueKey)} should have been defined.");

		return new CueData(
			CueKey,
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

		if (property["name"].AsStringName() == PropertyName.CueKey)
		{
			property["hint"] = (int)PropertyHint.Enum;
			property["hint_string"] = string.Join(",", GetCueOptions());
		}
	}

	private static string[] GetCueOptions()
	{
		ForgeData pluginData = ResourceLoader.Load<ForgeData>("uid://8j4xg16o3qnl");

		if (pluginData.RegisteredCues is null)
		{
			return [];
		}

		return [.. pluginData.RegisteredCues];
	}
#endif
}
