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

	[Export]
	public string? MagnitudeAttribute { get; set; }

	/// <summary>
	/// Gets or sets whether a client shows the cue on an entity it mirrors the moment its own predicted run applies
	/// the effect there, as a shot hitting an enemy. Default shows an executed cue early when the project setting
	/// <see cref="Core.ForgeSettings.PredictCuesOnOthersSetting"/> is on, and a persistent one when the server's word
	/// arrives.
	/// </summary>
	[ExportGroup("Networking")]
	[Export]
	public CuePrediction PredictOnOthers { get; set; }

	public CueData GetCueData()
	{
		return new CueData(
			CueKeys.GetTagContainer(),
			MinValue,
			MaxValue,
			MagnitudeType,
			string.IsNullOrEmpty(MagnitudeAttribute) ? null : MagnitudeAttribute,
			PredictOnOthers: PredictOnOthers);
	}

#if TOOLS
	public override void _ValidateProperty(Dictionary property)
	{
		if (property["name"].AsStringName() == PropertyName.MagnitudeAttribute
			&& (MagnitudeType == CueMagnitudeType.EffectLevel || MagnitudeType == CueMagnitudeType.StackCount))
		{
			property["usage"] = (int)PropertyUsageFlags.NoEditor;
		}
	}
#endif
}
