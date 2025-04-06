// Copyright © 2025 Gamesmiths Guild.

using Gamesmiths.Forge.Core.Godot;
using Godot;
using ForgeScalableFloat = Gamesmiths.Forge.GameplayEffects.Magnitudes.ScalableFloat;

namespace Gamesmiths.Forge.GameplayEffects.Magnitudes.Godot;

[Tool]
[GlobalClass]
public partial class ScalableFloat : Resource
{
	[Export]
	public float BaseValue { get; set; }

	[Export]
	public Curve ScalingCurve { get; set; }

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
