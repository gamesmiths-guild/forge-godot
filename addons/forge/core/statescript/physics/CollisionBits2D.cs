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
}
