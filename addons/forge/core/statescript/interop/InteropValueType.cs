// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Godot.Core.Statescript.Interop;

/// <summary>
/// The values the interop nodes and resolvers carry across the boundary between a graph and Godot's Variant.
/// </summary>
/// <remarks>
/// <para>The set is the eight authoring value types plus <see cref="Node"/>, and it is deliberately closed: a
/// conversion outside it is reported rather than guessed at, so a property path pointing at a colour or a resource
/// fails where it is authored instead of writing something meaningless.</para>
/// <para>There is no entity member. An entity crosses to Godot as the node it lives on, which is exactly what the Node
/// From Entity resolver spells, and back through Entity From Node - and spelling it keeps the conversion visible in the
/// graph rather than hiding it inside a dropdown entry.</para>
/// </remarks>
public enum InteropValueType
{
	/// <summary>
	/// No value. Used by the argument settings to say an argument is not passed.
	/// </summary>
	None = 0,

	/// <summary>
	/// A boolean.
	/// </summary>
	Bool = 1,

	/// <summary>
	/// A 32-bit signed integer.
	/// </summary>
#pragma warning disable CA1720 // Identifier contains type name
	Int = 2,

	/// <summary>
	/// A double-precision floating point number.
	/// </summary>
	Float = 3,
#pragma warning restore CA1720 // Identifier contains type name

	/// <summary>
	/// A 2D vector.
	/// </summary>
	Vector2 = 4,

	/// <summary>
	/// A 3D vector.
	/// </summary>
	Vector3 = 5,

	/// <summary>
	/// A 4D vector.
	/// </summary>
	Vector4 = 6,

	/// <summary>
	/// A plane.
	/// </summary>
	Plane = 7,

	/// <summary>
	/// A quaternion.
	/// </summary>
	Quaternion = 8,

	/// <summary>
	/// A reference to a scene node.
	/// </summary>
	Node = 9,
}
