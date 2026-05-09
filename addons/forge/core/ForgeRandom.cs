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
		for (int i = 0; i < buffer.Length; i++)
		{
			buffer[i] = (byte)_randomNumberGenerator.RandiRange(0, 255);
		}
	}

	public void NextBytes(Span<byte> buffer)
	{
		for (int i = 0; i < buffer.Length; i++)
		{
			buffer[i] = (byte)_randomNumberGenerator.RandiRange(0, 255);
		}
	}

	public double NextDouble()
	{
		double value;
		do
		{
			value = _randomNumberGenerator.Randf();
		}
		while (value >= 1.0d);

		return value;
	}

	public double NextDoubleInclusive()
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

	public int NextIntInclusive(int minValue, int maxValue)
	{
		return _randomNumberGenerator.RandiRange(minValue, maxValue);
	}

	public long NextInt64()
	{
		unchecked
		{
			uint high = _randomNumberGenerator.Randi();
			uint low = _randomNumberGenerator.Randi();
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

		ulong range = (ulong)(maxValue - minValue);
		ulong rand = (ulong)NextInt64();

		return (long)(rand % range) + minValue;
	}

	public long NextInt64Inclusive(long minValue, long maxValue)
	{
		if (minValue > maxValue)
		{
			throw new ArgumentOutOfRangeException(nameof(minValue), "minValue must be less than or equal to maxValue.");
		}

		if (minValue == maxValue)
		{
			return minValue;
		}

		if (maxValue == long.MaxValue)
		{
			ulong inclusiveRange = (ulong)(maxValue - minValue) + 1UL;
			ulong rand = (ulong)NextInt64();
			return (long)(rand % inclusiveRange) + minValue;
		}

		return NextInt64(minValue, maxValue + 1);
	}

	public float NextSingle()
	{
		float value;
		do
		{
			value = _randomNumberGenerator.Randf();
		}
		while (value >= 1.0f);

		return value;
	}

	public float NextSingleInclusive()
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
