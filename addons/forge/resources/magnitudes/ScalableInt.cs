// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core;
using Godot;
using ForgeScalableInt = Gamesmiths.Forge.GameplayEffects.Magnitudes.ScalableInt;

namespace Gamesmiths.Forge.Godot.Resources.Magnitudes;

[Tool]
[GlobalClass]
[Icon("uid://dnagt7tdo3dos")]
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
