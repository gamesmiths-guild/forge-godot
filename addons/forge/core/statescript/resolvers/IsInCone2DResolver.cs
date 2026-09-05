// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves whether a point falls inside a wedge.
/// </summary>
/// <remarks>
/// <para>The angle test Entities In Cone 2D runs, on its own so it composes. That resolver is the shortcut for the
/// common case — sweep a circle, keep what is in front — and this is the part it is built from, so anything the
/// shortcut cannot reach is still authorable: is what an area already found in front of me, is the entity I am
/// iterating within my facing, is the point the player clicked inside my firing arc.</para>
/// <para>Both share one angle test rather than carrying a copy each, so a Where over this can never disagree with the
/// query it is meant to mirror.</para>
/// <para>It takes a <em>point</em> rather than an entity, matching the sight resolvers: an entity's position is an
/// Entity Position 2D away, and a point also covers the cases with no entity to name.</para>
/// <para>The reach is optional, and <b>zero means unlimited</b> — the same reading a mask of zero gets. A filter
/// applied to an overlap that has already limited range only wants the aperture, and a reach that had to be repeated
/// there would be a number to keep in sync for no gain.</para>
/// </remarks>
/// <param name="pointResolver">Resolves the point being tested.</param>
/// <param name="originResolver">Resolves the wedge's apex.</param>
/// <param name="directionResolver">Resolves which way it opens.</param>
/// <param name="angleResolver">Resolves the full aperture, in degrees.</param>
/// <param name="rangeResolver">Resolves how far it reaches, or <see langword="null"/> for no limit.</param>
internal sealed class IsInCone2DResolver(
	IPropertyResolver pointResolver,
	IPropertyResolver originResolver,
	IPropertyResolver directionResolver,
	IPropertyResolver angleResolver,
	IPropertyResolver? rangeResolver) : IPropertyResolver
{
	private readonly IPropertyResolver _pointResolver = pointResolver;
	private readonly IPropertyResolver _originResolver = originResolver;
	private readonly IPropertyResolver _directionResolver = directionResolver;
	private readonly IPropertyResolver _angleResolver = angleResolver;
	private readonly IPropertyResolver? _rangeResolver = rangeResolver;

	public Type ValueType => typeof(bool);

	public Variant128 Resolve(GraphContext graphContext)
	{
		NumericsVector2 pointValue = _pointResolver.Resolve(graphContext).AsVector2();
		NumericsVector2 originValue = _originResolver.Resolve(graphContext).AsVector2();
		NumericsVector2 directionValue = _directionResolver.Resolve(graphContext).AsVector2();
		float angle = (float)_angleResolver.Resolve(graphContext).AsDouble();

		var direction = new Vector2(directionValue.X, directionValue.Y);

		// A wedge with no direction points nowhere and contains nothing, which is the honest answer for an operand
		// that resolved to zero.
		if (direction.LengthSquared() <= 0.000001f)
		{
			return new Variant128(false);
		}

		Vector2 offset = new Vector2(pointValue.X, pointValue.Y) - new Vector2(originValue.X, originValue.Y);

		float range = (float)(_rangeResolver?.Resolve(graphContext).AsDouble() ?? 0);

		if (range > 0 && offset.LengthSquared() > range * range)
		{
			return new Variant128(false);
		}

		return new Variant128(
			ConeQuery.IsWithinAngle(offset, direction.Normalized(), ConeQuery.ResolveCosHalfAngle(angle)));
	}
}
