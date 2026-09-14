// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using GdUnit4;
using NumericsQuaternion = System.Numerics.Quaternion;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Tests.Statescript.Resolvers;

/// <summary>
/// The Godot Look At faces −Z at its target where core's faces +Z. A node given core's answer looks exactly away from
/// what it was aimed at, so these pin the engine convention the rest of the spatial toolset follows.
/// </summary>
[TestSuite]
public class GodotLookAtResolverTests
{
	private const float Tolerance = 0.0001f;

	[TestCase]
	public void Minus_z_faces_the_target()
	{
		NumericsQuaternion rotation = Resolve(NumericsVector3.Zero, NumericsVector3.UnitX, NumericsVector3.UnitY);

		AssertApproximately(NumericsVector3.Transform(-NumericsVector3.UnitZ, rotation), NumericsVector3.UnitX);
		AssertApproximately(NumericsVector3.Transform(NumericsVector3.UnitY, rotation), NumericsVector3.UnitY);
	}

	[TestCase]
	public void Looking_down_minus_z_is_identity()
	{
		NumericsQuaternion rotation = Resolve(NumericsVector3.Zero, -NumericsVector3.UnitZ, NumericsVector3.UnitY);

		AssertApproximately(rotation, NumericsQuaternion.Identity);
	}

	[TestCase]
	public void A_target_on_the_origin_is_identity()
	{
		var origin = new NumericsVector3(2.0f, 3.0f, 4.0f);

		NumericsQuaternion rotation = Resolve(origin, origin, NumericsVector3.UnitY);

		AssertApproximately(rotation, NumericsQuaternion.Identity);
	}

	[TestCase]
	public void An_up_vector_along_the_line_of_sight_falls_back_to_world_up()
	{
		NumericsQuaternion rotation = Resolve(NumericsVector3.Zero, NumericsVector3.UnitX, NumericsVector3.UnitX);

		AssertApproximately(NumericsVector3.Transform(-NumericsVector3.UnitZ, rotation), NumericsVector3.UnitX);
		AssertApproximately(NumericsVector3.Transform(NumericsVector3.UnitY, rotation), NumericsVector3.UnitY);
	}

	[TestCase]
	public void A_vertical_line_of_sight_falls_back_to_world_right()
	{
		NumericsQuaternion rotation = Resolve(NumericsVector3.Zero, NumericsVector3.UnitY, NumericsVector3.UnitY);

		AssertApproximately(NumericsVector3.Transform(-NumericsVector3.UnitZ, rotation), NumericsVector3.UnitY);
		AssertApproximately(NumericsVector3.Transform(NumericsVector3.UnitY, rotation), NumericsVector3.UnitX);
	}

	private static NumericsQuaternion Resolve(NumericsVector3 from, NumericsVector3 to, NumericsVector3 up)
	{
		var resolver = new GodotLookAtResolver(
			new VariantResolver(new Variant128(from), typeof(NumericsVector3)),
			new VariantResolver(new Variant128(to), typeof(NumericsVector3)),
			new VariantResolver(new Variant128(up), typeof(NumericsVector3)));

		return resolver.Resolve(new GraphContext()).AsQuaternion();
	}

	private static void AssertApproximately(NumericsVector3 actual, NumericsVector3 expected)
	{
		actual.X.Should().BeApproximately(expected.X, Tolerance);
		actual.Y.Should().BeApproximately(expected.Y, Tolerance);
		actual.Z.Should().BeApproximately(expected.Z, Tolerance);
	}

	private static void AssertApproximately(NumericsQuaternion actual, NumericsQuaternion expected)
	{
		actual.X.Should().BeApproximately(expected.X, Tolerance);
		actual.Y.Should().BeApproximately(expected.Y, Tolerance);
		actual.Z.Should().BeApproximately(expected.Z, Tolerance);
		actual.W.Should().BeApproximately(expected.W, Tolerance);
	}
}
