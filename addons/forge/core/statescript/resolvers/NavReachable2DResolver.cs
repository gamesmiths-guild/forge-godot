// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves whether a destination can be walked to from a point.
/// </summary>
/// <remarks>
/// <para>Navigation was a node and never a question: Nav Move To walks, and a graph that wanted to know <em>before</em>
/// committing had to start the walk and wait for it to fail. This is the question on its own - an AI branching to a
/// ranged attack because it cannot path to you, a summon refusing an order that would strand it, a charge that checks
/// the corridor exists before playing its wind-up.</para>
/// <para>Reachable means the path arrives, not that a path came back. Godot answers an impossible request with a path
/// to the closest point it could reach, so a destination across a gap returns a perfectly good path that stops at the
/// edge - the test is whether its last point is near the one that was asked for.</para>
/// <para>Near is <see cref="Tolerance"/>, unbound at Godot's own agent default, and it has to be forgiving: a
/// destination is snapped onto the navigation mesh before the path is built, so even an obviously reachable point
/// rarely comes back exactly. It is in pixels here, which is why the default is larger than the 3D one.</para>
/// <para>Both ends are points, matching Line Of Sight 2D. An entity's position is one resolver away, and a point is
/// strictly more - a click on the ground has no entity to name.</para>
/// </remarks>
/// <param name="fromResolver">Resolves where the walk would start.</param>
/// <param name="toResolver">Resolves where it would end.</param>
/// <param name="toleranceResolver">Resolves how near the path has to land, or <see langword="null"/> for the
/// default.</param>
internal sealed class NavReachable2DResolver(
	IPropertyResolver fromResolver,
	IPropertyResolver toResolver,
	IPropertyResolver? toleranceResolver) : IPropertyResolver
{
	/// <summary>
	/// How near the path has to end to count as arriving, when nothing is bound. This is
	/// <c>NavigationAgent2D.target_desired_distance</c>'s own default, so an unbound check agrees with what an agent
	/// walking the same path would report.
	/// </summary>
	public const double Tolerance = 10.0;

	private readonly IPropertyResolver _fromResolver = fromResolver;
	private readonly IPropertyResolver _toResolver = toResolver;
	private readonly IPropertyResolver? _toleranceResolver = toleranceResolver;

	public Type ValueType => typeof(bool);

	public Variant128 Resolve(GraphContext graphContext)
	{
		World2D? world = PhysicsQuery2D.ResolveWorld(graphContext);

		if (world is null)
		{
			return new Variant128(false);
		}

		NumericsVector2 fromValue = _fromResolver.Resolve(graphContext).AsVector2();
		NumericsVector2 toValue = _toResolver.Resolve(graphContext).AsVector2();

		var to = new Vector2(toValue.X, toValue.Y);

		Vector2[] path = NavigationServer2D.MapGetPath(
			world.NavigationMap,
			new Vector2(fromValue.X, fromValue.Y),
			to,
			true);

		double tolerance = _toleranceResolver is null
			? Tolerance
			: _toleranceResolver.Resolve(graphContext).AsDouble();

		return new Variant128(path.Length > 0 && path[^1].DistanceTo(to) <= tolerance);
	}
}
