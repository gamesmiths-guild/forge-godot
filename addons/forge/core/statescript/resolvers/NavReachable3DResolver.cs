// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using NumericsVector3 = System.Numerics.Vector3;

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
/// to the closest point it could reach, so a destination across a chasm returns a perfectly good path that stops at
/// the edge - the test is whether its last point is near the one that was asked for.</para>
/// <para>Near is <see cref="Tolerance"/>, unbound at Godot's own agent default, and it has to be forgiving: a
/// destination is snapped onto the navigation mesh before the path is built, so even an obviously reachable point
/// rarely comes back exactly. Raise it for coarse meshes, lower it when a walk has to land precisely.</para>
/// <para>Both ends are points, matching Line Of Sight 3D. An entity's position is one resolver away, and a point is
/// strictly more - a click on the ground has no entity to name.</para>
/// </remarks>
/// <param name="fromResolver">Resolves where the walk would start.</param>
/// <param name="toResolver">Resolves where it would end.</param>
/// <param name="toleranceResolver">Resolves how near the path has to land, or <see langword="null"/> for the
/// default.</param>
internal sealed class NavReachable3DResolver(
	IPropertyResolver fromResolver,
	IPropertyResolver toResolver,
	IPropertyResolver? toleranceResolver) : IPropertyResolver
{
	/// <summary>
	/// How near the path has to end to count as arriving, when nothing is bound. This is
	/// <c>NavigationAgent3D.target_desired_distance</c>'s own default, so an unbound check agrees with what an agent
	/// walking the same path would report.
	/// </summary>
	public const double Tolerance = 1.0;

	private readonly IPropertyResolver _fromResolver = fromResolver;
	private readonly IPropertyResolver _toResolver = toResolver;
	private readonly IPropertyResolver? _toleranceResolver = toleranceResolver;

	public Type ValueType => typeof(bool);

	public Variant128 Resolve(GraphContext graphContext)
	{
		World3D? world = PhysicsQuery3D.ResolveWorld(graphContext);

		if (world is null)
		{
			return new Variant128(false);
		}

		NumericsVector3 fromValue = _fromResolver.Resolve(graphContext).AsVector3();
		NumericsVector3 toValue = _toResolver.Resolve(graphContext).AsVector3();

		var from = new Vector3(fromValue.X, fromValue.Y, fromValue.Z);
		var to = new Vector3(toValue.X, toValue.Y, toValue.Z);

		// Nothing is reachable across a map that has not synchronized yet, which is the honest answer and the one the
		// query would arrive at anyway - but asking it directly is what the engine prints an error about, and a graph
		// evaluated on the frame its level loaded would raise that error in a perfectly navigable scene.
		if (NavigationServer3D.MapGetIterationId(world.NavigationMap) == 0)
		{
			return new Variant128(false);
		}

		Vector3[] path = NavigationServer3D.MapGetPath(world.NavigationMap, from, to, true);

		double tolerance = _toleranceResolver is null
			? Tolerance
			: _toleranceResolver.Resolve(graphContext).AsDouble();

		return new Variant128(path.Length > 0 && path[^1].DistanceTo(to) <= tolerance);
	}
}
