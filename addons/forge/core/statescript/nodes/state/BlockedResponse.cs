// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// What a solving move does with the part of its step a collision refused.
/// </summary>
/// <remarks>
/// This is the whole difference between a dash that reads as an impact and one that reads as a scramble, which is why
/// it is authored rather than assumed. Neither answer changes how long the move lasts: both end at the destination or
/// at the move's own duration, whichever comes first.
/// </remarks>
public enum BlockedResponse
{
	/// <summary>
	/// The move ends at the first thing it meets, reporting what stopped it. A charge that hits, a leap that slams
	/// into a wall and drops.
	/// </summary>
	Stop = 0,

	/// <summary>
	/// The refused part of the step is redirected along the surface and the move carries on. A dash that grazes a
	/// corner and keeps going rather than dying on it.
	/// </summary>
	Slide = 1,
}
