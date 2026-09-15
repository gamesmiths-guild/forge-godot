// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using NumericsQuaternion = System.Numerics.Quaternion;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// The Look At resolver in Godot's own terms: a quaternion whose −Z points from one position at another.
/// </summary>
/// <remarks>
/// <para>Core's <see cref="LookAtResolver"/> builds its rotation with +Z as forward, which is as good a convention as
/// any for a library with no engine behind it. Godot's forward is −Z, and everything here that reads or writes a
/// facing follows the engine: Entity Direction 3D, Set Rotation Toward 3D, Look At 3D, a <c>ForgeProjectile3D</c> in
/// flight. Written to a node as-is, core's answer faces exactly away from the target, so a projectile spawned with it
/// flies backwards. This builds the rotation with <see cref="Basis.LookingAt"/>, the same call the facing nodes make,
/// so its answer can be written to a node and agrees with what Entity Rotation 3D reads back.</para>
/// <para>The fallbacks for an unusable up vector are core's: one along the line of sight gives way to world up, and a
/// vertical line of sight to world right. A zero-length line of sight resolves to identity, as it does in core.</para>
/// </remarks>
/// <param name="from">Resolves the position looked from.</param>
/// <param name="to">Resolves the position looked at.</param>
/// <param name="up">Resolves the up vector.</param>
internal sealed class GodotLookAtResolver(IPropertyResolver from, IPropertyResolver to, IPropertyResolver up)
	: IPropertyResolver
{
	private readonly IPropertyResolver _from = from;
	private readonly IPropertyResolver _to = to;
	private readonly IPropertyResolver _up = up;

	/// <inheritdoc/>
	public Type ValueType { get; } = ValidateTypes(from.ValueType, to.ValueType, up.ValueType);

	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		NumericsVector3 fromValue = _from.Resolve(graphContext).AsVector3();
		NumericsVector3 toValue = _to.Resolve(graphContext).AsVector3();
		NumericsVector3 upValue = _up.Resolve(graphContext).AsVector3();

		var direction = new Vector3(toValue.X - fromValue.X, toValue.Y - fromValue.Y, toValue.Z - fromValue.Z);

		if (direction.IsZeroApprox())
		{
			return new Variant128(NumericsQuaternion.Identity);
		}

		direction = direction.Normalized();

		// LookingAt rejects an up vector parallel to the line of sight, and a zero one; both fall through to the
		// same world axes core falls back to.
		var upAxis = new Vector3(upValue.X, upValue.Y, upValue.Z);

		if (upAxis.Cross(direction).IsZeroApprox())
		{
			upAxis = Vector3.Up;
		}

		if (upAxis.Cross(direction).IsZeroApprox())
		{
			upAxis = Vector3.Right;
		}

		Quaternion rotation = Basis.LookingAt(direction, upAxis).GetRotationQuaternion();

		return new Variant128(new NumericsQuaternion(rotation.X, rotation.Y, rotation.Z, rotation.W));
	}

	private static Type ValidateTypes(Type fromType, Type toType, Type upType)
	{
		if (fromType != typeof(NumericsVector3)
			|| toType != typeof(NumericsVector3)
			|| upType != typeof(NumericsVector3))
		{
			throw new ArgumentException(
				"GodotLookAtResolver requires from, to, and up to all be Vector3. " +
				$"Got '{fromType}', '{toType}', and '{upType}'.");
		}

		return typeof(NumericsQuaternion);
	}
}
