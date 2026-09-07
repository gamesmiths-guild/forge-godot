// Copyright © Gamesmiths Guild.

using System;

namespace Gamesmiths.Forge.Godot.Core.Statescript;

/// <summary>
/// Marks the input properties of a node type that carry an angle, so the editor authors them in degrees.
/// </summary>
/// <remarks>
/// <para>The value the node reads is still radians, which is what the runtime and core's numeric toolbox speak. Only
/// the constant field changes: it shows the degrees a designer types and stores the radians they mean, the same way
/// Godot's own inspector presents <c>Node2D.Rotation</c>. A slot filled with a resolver instead of a constant is
/// untouched, because that value was computed rather than typed.</para>
/// <para>Only mark an input whose unit is fixed. An input that means radians under one node configuration and seconds
/// under another - a rate that doubles as a duration - has no single unit to declare and is left alone.</para>
/// </remarks>
/// <param name="inputIndices">The indices of the input properties that carry an angle.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class StatescriptAngleInputsAttribute(params int[] inputIndices) : Attribute
{
	private readonly int[] _inputIndices = inputIndices ?? [];

	/// <summary>
	/// Checks whether the input property at the given index carries an angle.
	/// </summary>
	/// <param name="inputIndex">The index of the input property to check.</param>
	/// <returns><see langword="true"/> when the input is authored in degrees.</returns>
	public bool IsAngleInput(int inputIndex)
	{
		for (int i = 0; i < _inputIndices.Length; i++)
		{
			if (_inputIndices[i] == inputIndex)
			{
				return true;
			}
		}

		return false;
	}
}
