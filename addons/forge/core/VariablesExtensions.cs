// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using GodotPlane = Godot.Plane;
using GodotQuaternion = Godot.Quaternion;
using GodotVector2 = Godot.Vector2;
using GodotVector3 = Godot.Vector3;
using GodotVector4 = Godot.Vector4;
using SysPlane = System.Numerics.Plane;
using SysQuaternion = System.Numerics.Quaternion;
using SysVector2 = System.Numerics.Vector2;
using SysVector3 = System.Numerics.Vector3;
using SysVector4 = System.Numerics.Vector4;

namespace Gamesmiths.Forge.Godot.Core;

/// <summary>
/// Extension methods for <see cref="Variables"/> that provide seamless support for Godot types. These methods
/// automatically convert Godot math types (e.g., <see cref="GodotVector3"/>) to their System.Numerics equivalents
/// before storing them in the variable bag.
/// </summary>
/// <remarks>
/// Use these overloads in data binder delegates (e.g., when implementing
/// <see cref="Resources.IActivationDataProvider.CreateBehavior"/>) to avoid manual Godot-to-System.Numerics
/// conversions.
/// </remarks>
public static class VariablesExtensions
{
	/// <summary>
	/// Sets a variable from a <see cref="GodotVector2"/> value, converting it to <see cref="SysVector2"/>.
	/// </summary>
	/// <param name="variables">The variables bag.</param>
	/// <param name="name">The name of the variable to set.</param>
	/// <param name="value">The Godot Vector2 value to store.</param>
	public static void SetGodotVar(this Variables variables, StringKey name, GodotVector2 value)
	{
		variables.SetVariant(name, new Variant128(new SysVector2(value.X, value.Y)));
	}

	/// <summary>
	/// Sets a variable from a <see cref="GodotVector3"/> value, converting it to <see cref="SysVector3"/>.
	/// </summary>
	/// <param name="variables">The variables bag.</param>
	/// <param name="name">The name of the variable to set.</param>
	/// <param name="value">The Godot Vector3 value to store.</param>
	public static void SetGodotVar(this Variables variables, StringKey name, GodotVector3 value)
	{
		variables.SetVariant(name, new Variant128(new SysVector3(value.X, value.Y, value.Z)));
	}

	/// <summary>
	/// Sets a variable from a <see cref="GodotVector4"/> value, converting it to <see cref="SysVector4"/>.
	/// </summary>
	/// <param name="variables">The variables bag.</param>
	/// <param name="name">The name of the variable to set.</param>
	/// <param name="value">The Godot Vector4 value to store.</param>
	public static void SetGodotVar(this Variables variables, StringKey name, GodotVector4 value)
	{
		variables.SetVariant(name, new Variant128(new SysVector4(value.X, value.Y, value.Z, value.W)));
	}

	/// <summary>
	/// Sets a variable from a <see cref="GodotPlane"/> value, converting it to <see cref="SysPlane"/>.
	/// </summary>
	/// <param name="variables">The variables bag.</param>
	/// <param name="name">The name of the variable to set.</param>
	/// <param name="value">The Godot Plane value to store.</param>
	public static void SetGodotVar(this Variables variables, StringKey name, GodotPlane value)
	{
		variables.SetVariant(
			name,
			new Variant128(new SysPlane(value.Normal.X, value.Normal.Y, value.Normal.Z, value.D)));
	}

	/// <summary>
	/// Sets a variable from a <see cref="GodotQuaternion"/> value, converting it to <see cref="SysQuaternion"/>.
	/// </summary>
	/// <param name="variables">The variables bag.</param>
	/// <param name="name">The name of the variable to set.</param>
	/// <param name="value">The Godot Quaternion value to store.</param>
	public static void SetGodotVar(this Variables variables, StringKey name, GodotQuaternion value)
	{
		variables.SetVariant(name, new Variant128(new SysQuaternion(value.X, value.Y, value.Z, value.W)));
	}
}
