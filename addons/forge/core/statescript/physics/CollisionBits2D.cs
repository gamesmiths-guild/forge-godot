// Copyright © Gamesmiths Guild.

using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Physics;

/// <summary>
/// Reading and writing the two collision bit fields, shared by the node that writes them permanently and the one that
/// restores them.
/// </summary>
internal static class CollisionBits2D
{
	/// <summary>
	/// Reads the bit field named by <paramref name="target"/>.
	/// </summary>
	/// <param name="body">The collision object to read.</param>
	/// <param name="target">Which field to read.</param>
	/// <returns>The current bits.</returns>
	public static uint Read(CollisionObject2D body, CollisionBitsTarget target)
	{
		return target == CollisionBitsTarget.Mask ? body.CollisionMask : body.CollisionLayer;
	}

	/// <summary>
	/// Writes the bit field named by <paramref name="target"/>.
	/// </summary>
	/// <param name="body">The collision object to write.</param>
	/// <param name="target">Which field to write.</param>
	/// <param name="bits">The bits to write.</param>
	public static void Write(CollisionObject2D body, CollisionBitsTarget target, uint bits)
	{
		if (target == CollisionBitsTarget.Mask)
		{
			body.CollisionMask = bits;
			return;
		}

		body.CollisionLayer = bits;
	}

	/// <summary>
	/// Applies an operation to the named bits, leaving every other bit alone.
	/// </summary>
	/// <param name="current">The current field value.</param>
	/// <param name="bits">The bits the operation acts on.</param>
	/// <param name="operation">What to do to them.</param>
	/// <returns>The new field value.</returns>
	public static uint Apply(uint current, uint bits, CollisionBitsOperation operation)
	{
		return operation == CollisionBitsOperation.Set ? current | bits : current & ~bits;
	}

	/// <summary>
	/// Puts the named bits back to what they were, leaving every other bit as it currently stands.
	/// </summary>
	/// <remarks>
	/// An override restores its own bits rather than the whole field it captured, because the field it captured is a
	/// snapshot of a moment that has since moved on. Writing the snapshot back would undo every other change made while
	/// the override was running - a second override on different bits of the same field, or a permanent write - and
	/// resurrect the bits those had deliberately changed.
	/// </remarks>
	/// <param name="current">The field as it stands now.</param>
	/// <param name="original">The field as it was when the override captured it.</param>
	/// <param name="bits">The bits the override acted on.</param>
	/// <returns>The new field value.</returns>
	public static uint Restore(uint current, uint original, uint bits)
	{
		return (current & ~bits) | (original & bits);
	}
}
