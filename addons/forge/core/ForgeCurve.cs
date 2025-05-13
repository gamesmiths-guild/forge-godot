// Copyright © Gamesmiths Guild.

using Godot;

namespace Gamesmiths.Forge.Core.Godot;

public readonly struct ForgeCurve(Curve? curve) : ICurve
{
	private readonly Curve? _curve = curve;

	public float Evaluate(float value)
	{
		if (_curve is null)
		{
			return 1;
		}

		return _curve.Sample(value);
	}
}
