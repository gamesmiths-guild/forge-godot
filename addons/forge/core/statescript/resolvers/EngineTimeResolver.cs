// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves how long the game has been running, in seconds.
/// </summary>
/// <remarks>
/// <para>The stamp a graph writes into a variable so it can measure the gap to the next one: a combo window that only
/// counts if the second press is within so long of the first, an ability that scales with how long since the last hit,
/// a cooldown a graph tracks itself. Statescript has timers for waiting, and this is the other half - for arithmetic
/// on when things happened.</para>
/// <para>It is monotonic engine time, not wall clock. Two stamps therefore always subtract to a real interval, where
/// two clock readings can subtract to a negative one across a time-zone change or a clock correction, and the answer
/// is the same on every machine a networked game runs on.</para>
/// <para>Read in microseconds and converted, so a stamp is precise well below a frame - the intervals it is subtracted
/// into are often shorter than one.</para>
/// </remarks>
internal sealed class EngineTimeResolver : IPropertyResolver
{
	private const double MicrosecondsPerSecond = 1000000.0;

	public Type ValueType => typeof(double);

	public Variant128 Resolve(GraphContext graphContext)
	{
		return new Variant128(Time.GetTicksUsec() / MicrosecondsPerSecond);
	}
}
