// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Nodes;
using Godot;
using ForgeScalableFloat = Gamesmiths.Forge.GameplayEffects.Magnitudes.ScalableFloat;

namespace Gamesmiths.Forge.Godot.Resources.Magnitudes;

[Tool]
[GlobalClass]
[Icon("uid://dvlaw4yolashm")]
public partial class ScalableFloat : ForgeResource
{
	[Export]
	public float BaseValue { get; set; }

	[Export]
	public Curve? ScalingCurve { get; set; }

	public ScalableFloat()
	{
		// Constructor intentionally left blank.
	}

	public ScalableFloat(float baseValue)
	{
		BaseValue = baseValue;
	}

	public ForgeScalableFloat GetScalableFloat()
	{
		return new ForgeScalableFloat(BaseValue, new ForgeCurve(ScalingCurve));
	}
}
