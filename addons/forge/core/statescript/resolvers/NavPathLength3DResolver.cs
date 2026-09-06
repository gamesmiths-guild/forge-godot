// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves how far the walk between two points actually is.
/// </summary>
/// <remarks>
/// <para>The distance that matters for anything that walks. Straight-line distance says a target on the far side of a
/// wall is close; this says how far the legs have to carry you, which is what decides whether an AI charges or
/// repositions, which of two objectives is really nearer, and whether a chase is worth starting.</para>
/// <para>Measured along the path Godot returns, summed corner to corner. A destination that cannot be reached is not
/// an error here: the answer is the length of the walk to the closest point the path could get to, which is the honest
/// reading of "how far can I get". Pair it with Nav Reachable 3D when the difference matters.</para>
/// <para>Zero when there is no path at all - an origin off the navigation mesh entirely, or no mesh in the scene -
/// which reads the same as being already there. That is the same conflation Godot's own path API makes, and it is why
/// a graph branching on distance should ask Nav Reachable 3D first.</para>
/// </remarks>
/// <param name="fromResolver">Resolves where the walk would start.</param>
/// <param name="toResolver">Resolves where it would end.</param>
internal sealed class NavPathLength3DResolver(IPropertyResolver fromResolver, IPropertyResolver toResolver)
	: IPropertyResolver
{
	private readonly IPropertyResolver _fromResolver = fromResolver;
	private readonly IPropertyResolver _toResolver = toResolver;

	public Type ValueType => typeof(double);

	public Variant128 Resolve(GraphContext graphContext)
	{
		World3D? world = PhysicsQuery3D.ResolveWorld(graphContext);

		if (world is null)
		{
			return new Variant128(0.0);
		}

		// No walk at all across a map that has not synchronized yet, which is what an empty path would report anyway -
		// but asking it directly is what the engine prints an error about, and a graph evaluated on the frame its
		// level loaded would raise that error in a perfectly navigable scene.
		if (NavigationServer3D.MapGetIterationId(world.NavigationMap) == 0)
		{
			return new Variant128(0.0);
		}

		NumericsVector3 fromValue = _fromResolver.Resolve(graphContext).AsVector3();
		NumericsVector3 toValue = _toResolver.Resolve(graphContext).AsVector3();

		Vector3[] path = NavigationServer3D.MapGetPath(
			world.NavigationMap,
			new Vector3(fromValue.X, fromValue.Y, fromValue.Z),
			new Vector3(toValue.X, toValue.Y, toValue.Z),
			true);

		double length = 0.0;

		for (int i = 1; i < path.Length; i++)
		{
			length += path[i - 1].DistanceTo(path[i]);
		}

		return new Variant128(length);
	}
}
