// Copyright © 2025 Gamesmiths Guild.

using Gamesmiths.Forge.Core.Godot;
using Godot;
using ForgeScalableInt = Gamesmiths.Forge.GameplayEffects.Magnitudes.ScalableInt;

namespace Gamesmiths.Forge.GameplayEffects.Magnitudes.Godot;

[Tool]
[GlobalClass]
public partial class ScalableInt : Resource
{
	[Export]
	public int BaseValue { get; set; }

	[Export]
	public Curve? ScalingCurve { get; set; }

	public ScalableInt()
	{
		// Constructor intentionally left blank.
	}

	public ScalableInt(int baseValue)
	{
		BaseValue = baseValue;
	}

	public ForgeScalableInt GetScalableInt()
	{
		return new ForgeScalableInt(BaseValue, new ForgeCurve(ScalingCurve));
	}
}
