// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Godot;

namespace Gamesmiths.Forge.Godot.Core;

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
