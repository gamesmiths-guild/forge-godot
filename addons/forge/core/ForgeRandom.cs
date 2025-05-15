// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Core;
using Godot;

namespace Gamesmiths.Forge.Godot.Core;

public class ForgeRandom : IRandom, IDisposable
{
	private readonly RandomNumberGenerator _randomNumberGenerator;

	public ForgeRandom()
	{
		_randomNumberGenerator = new RandomNumberGenerator();
		_randomNumberGenerator.Randomize();
	}

	public void NextBytes(byte[] buffer)
	{
		for (var i = 0; i < buffer.Length; i++)
		{
			buffer[i] = (byte)_randomNumberGenerator.RandiRange(0, 255);
		}
	}

	public void NextBytes(Span<byte> buffer)
	{
		for (var i = 0; i < buffer.Length; i++)
		{
			buffer[i] = (byte)_randomNumberGenerator.RandiRange(0, 255);
		}
	}

	public double NextDouble()
	{
		return _randomNumberGenerator.Randf();
	}

	public int NextInt()
	{
		return (int)_randomNumberGenerator.Randi();
	}

	public int NextInt(int maxValue)
	{
		return _randomNumberGenerator.RandiRange(0, maxValue - 1);
	}

	public int NextInt(int minValue, int maxValue)
	{
		return _randomNumberGenerator.RandiRange(minValue, maxValue - 1);
	}

	public long NextInt64()
	{
		unchecked
		{
			var high = _randomNumberGenerator.Randi();
			var low = _randomNumberGenerator.Randi();
			return ((long)high << 32) | low;
		}
	}

	public long NextInt64(long maxValue)
	{
		return NextInt64(0, maxValue);
	}

	public long NextInt64(long minValue, long maxValue)
	{
		if (minValue >= maxValue)
		{
			throw new ArgumentOutOfRangeException(nameof(minValue), "minValue must be less than maxValue.");
		}

		var range = (ulong)(maxValue - minValue);
		var rand = (ulong)NextInt64();

		return (long)(rand % range) + minValue;
	}

	public float NextSingle()
	{
		return _randomNumberGenerator.Randf();
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		_randomNumberGenerator.Dispose();
	}
}
