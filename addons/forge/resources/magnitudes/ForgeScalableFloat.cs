// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Godot.Core;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Magnitudes;

[Tool]
[GlobalClass]
[Icon("uid://dvlaw4yolashm")]
public partial class ForgeScalableFloat : Resource
{
	[Export]
	public float BaseValue { get; set; }

	[Export]
	public Curve? ScalingCurve { get; set; }

	public ForgeScalableFloat()
	{
		// Constructor intentionally left blank.
	}

	public ForgeScalableFloat(float baseValue)
	{
		BaseValue = baseValue;
	}

	public ScalableFloat GetScalableFloat()
	{
		return new ScalableFloat(BaseValue, new ForgeCurve(ScalingCurve));
	}
}
