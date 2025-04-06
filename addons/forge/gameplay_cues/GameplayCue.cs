// Copyright © 2025 Gamesmiths Guild.

using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.GameplayCues.Godot;

[Tool]
[GlobalClass]
public partial class GameplayCue : Resource
{
	private CueMagnitudeType _magnitudeType;

	[Export]
	public string CueKey { get; set; }

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

	[Export]
	public string MagnitudeAttribute { get; set; }

	public GameplayCueData GetGameplayCueData()
	{
		return new GameplayCueData(
			CueKey,
			MinValue,
			MaxValue,
			MagnitudeType,
			string.IsNullOrEmpty(MagnitudeAttribute) ? null : MagnitudeAttribute);
	}

	public override void _ValidateProperty(Dictionary property)
	{
		if ((MagnitudeType == CueMagnitudeType.EffectLevel || MagnitudeType == CueMagnitudeType.StackCount)
			&& property["name"].AsStringName() == PropertyName.MagnitudeAttribute)
		{
			property["usage"] = (int)(PropertyUsageFlags.Default | PropertyUsageFlags.ReadOnly);
		}
	}
}
