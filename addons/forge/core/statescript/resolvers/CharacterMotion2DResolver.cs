// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves one of the vectors a character body reports about its last move.
/// </summary>
/// <remarks>
/// <para>Like Character State 2D, these are methods rather than properties, so no expression could reach them before.
/// Together they are what makes contact-dependent abilities authorable: a wall-jump aims off
/// <see cref="CharacterMotionValue.WallNormal"/>, a slope-aware landing reads
/// <see cref="CharacterMotionValue.FloorNormal"/>, and a hit that scales with impact speed reads
/// <see cref="CharacterMotionValue.RealVelocity"/>.</para>
/// <para><b>Real velocity is the one Entity Velocity 2D cannot give.</b> That resolver reads the velocity the game
/// <em>asked</em> for, which for a character walking into a wall is a full-speed vector into geometry it never moved
/// through. This reads what the slide actually achieved.</para>
/// <para>Only a <see cref="CharacterBody2D"/> reports these. Anything else resolves to zero.</para>
/// </remarks>
/// <param name="entityResolver">Resolves which entity to read.</param>
/// <param name="nodePath">Optional path to a descendant node to read instead.</param>
/// <param name="value">Which reading to report.</param>
internal sealed class CharacterMotion2DResolver(
	IEntityResolver entityResolver,
	string nodePath,
	CharacterMotionValue value) : SpatialResolverBase2D(entityResolver, nodePath)
{
	private readonly CharacterMotionValue _value = value;

	public override Type ValueType => typeof(NumericsVector2);

	protected override Variant128 ResolveFrom(Node2D spatialNode, GraphContext graphContext)
	{
		if (spatialNode is not CharacterBody2D characterBody)
		{
			ReportUnusableNodeOnce(
				$"resolved to a {spatialNode.GetType().Name}, which reports no slide motion - only a" +
				" CharacterBody2D does. Resolving to zero.");

			return new Variant128(NumericsVector2.Zero);
		}

		// The normals are guarded because Godot's own getters are only meaningful while the corresponding contact
		// holds; off the floor, the floor normal is whatever the last one was.
		Vector2 result = _value switch
		{
			CharacterMotionValue.FloorNormal =>
				characterBody.IsOnFloor() ? characterBody.GetFloorNormal() : Vector2.Zero,
			CharacterMotionValue.WallNormal =>
				characterBody.IsOnWall() ? characterBody.GetWallNormal() : Vector2.Zero,
			CharacterMotionValue.LastMotion => characterBody.GetLastMotion(),
			CharacterMotionValue.PositionDelta => characterBody.GetPositionDelta(),
			CharacterMotionValue.PlatformVelocity => characterBody.GetPlatformVelocity(),
			_ => characterBody.GetRealVelocity(),
		};

		return new Variant128(new NumericsVector2(result.X, result.Y));
	}
}
