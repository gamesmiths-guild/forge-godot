// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;

namespace Gamesmiths.Forge.Godot.Core.Statescript;

/// <summary>
/// Marks the input properties of a node type that carry a collision layer or mask, so the editor authors them as the
/// bit grid Godot's own inspector uses rather than as a number.
/// </summary>
/// <remarks>
/// <para>The value the node reads is still the integer the physics server is handed. Only the way it is typed changes:
/// a layer field is a set of thirty-two bits, so <c>12</c> means the third and fourth layers rather than the twelfth,
/// which is a translation nobody should be doing in their head.</para>
/// <para>The layer space says which of the two name sets the grid reads. It belongs to the node rather than to the
/// resolver because a 2D node's mask is never anything but 2D, and asking again in the resolver would only be a chance
/// to answer wrongly.</para>
/// </remarks>
/// <param name="layerSpace">Which world's layers the marked inputs are picked from.</param>
/// <param name="inputIndices">The indices of the input properties that carry a collision layer or mask.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class StatescriptCollisionMaskInputsAttribute(
	CollisionLayerSpace layerSpace,
	params int[] inputIndices) : Attribute
{
	private readonly int[] _inputIndices = inputIndices ?? [];

	/// <summary>
	/// Gets the layer space the input property at the given index is picked from.
	/// </summary>
	/// <param name="inputIndex">The index of the input property to check.</param>
	/// <returns>The layer space, or <see cref="CollisionLayerSpace.None"/> when the input is an ordinary integer.
	/// </returns>
	public CollisionLayerSpace GetLayerSpace(int inputIndex)
	{
		for (int i = 0; i < _inputIndices.Length; i++)
		{
			if (_inputIndices[i] == inputIndex)
			{
				return layerSpace;
			}
		}

		return CollisionLayerSpace.None;
	}
}
