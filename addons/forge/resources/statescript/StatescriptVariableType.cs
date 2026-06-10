// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Godot.Resources.Statescript;

/// <summary>
/// Enumerates the supported variable types for Statescript graph variables and node properties.
/// Maps directly to the types supported by Forge's <see cref="Forge.Statescript.Variant128"/>.
/// </summary>
public enum StatescriptVariableType
{
	/// <summary>Boolean value.</summary>
	Bool = 0,

	/// <summary>Unsigned 8-bit integer.</summary>
	Byte = 1,

	/// <summary>Signed 8-bit integer.</summary>
	SByte = 2,

	/// <summary>Unicode character.</summary>
#pragma warning disable CA1720 // Identifier contains type name
	Char = 3,

	/// <summary>128-bit decimal.</summary>
	Decimal = 4,

	/// <summary>64-bit floating point.</summary>
	Double = 5,

	/// <summary>32-bit floating point.</summary>
	Float = 6,

	/// <summary>Signed 32-bit integer.</summary>
	Int = 7,

	/// <summary>Unsigned 32-bit integer.</summary>
	UInt = 8,

	/// <summary>Signed 64-bit integer.</summary>
	Long = 9,

	/// <summary>Unsigned 64-bit integer.</summary>
	ULong = 10,

	/// <summary>Signed 16-bit integer.</summary>
	Short = 11,

	/// <summary>Unsigned 16-bit integer.</summary>
	UShort = 12,
#pragma warning restore CA1720 // Identifier contains type name

	/// <summary>2D vector (float x, float y).</summary>
	Vector2 = 13,

	/// <summary>3D vector (float x, float y, float z).</summary>
	Vector3 = 14,

	/// <summary>4D vector (float x, float y, float z, float w).</summary>
	Vector4 = 15,

	/// <summary>Plane (normal + distance).</summary>
	Plane = 16,

	/// <summary>Quaternion rotation.</summary>
	Quaternion = 17,
}
