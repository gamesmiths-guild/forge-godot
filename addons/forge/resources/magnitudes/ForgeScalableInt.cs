// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Godot.Core;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Magnitudes;

[Tool]
[GlobalClass]
[Icon("uid://dnagt7tdo3dos")]
public partial class ForgeScalableInt : Resource
{
	[Export]
	public int BaseValue { get; set; }

	[Export]
	public Curve? ScalingCurve { get; set; }

	public ForgeScalableInt()
	{
		// Constructor intentionally left blank.
	}

	public ForgeScalableInt(int baseValue)
	{
		BaseValue = baseValue;
	}

	public ScalableInt GetScalableInt()
	{
		return new ScalableInt(BaseValue, new ForgeCurve(ScalingCurve));
	}
}
