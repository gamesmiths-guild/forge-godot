// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Networking.Replication;
using Godot;
using NumericsQuaternion = System.Numerics.Quaternion;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core;

// Stands bodies where an instigator was shown them, for the queries of a lag-compensated hit. Godot moves a kinematic
// body only on its next physics step, so the live bodies can't be moved back; each gets a static body of its own in the
// physics server instead, made of the same shapes as they are now, which takes the live body's layers while the live
// one has none. A hit on it reports the live node, whose instance it carries, so whatever reads the hit finds the
// entity as ever.
internal sealed class LagCompensationProxies
{
	// The stand-in of each live body placed now, so a query that leaves a body out leaves its stand-in out too, and a
	// layer gameplay sets meanwhile reaches the stand-in rather than the live body.
	private static readonly Dictionary<Rid, IProxy> _standIns = [];

	private readonly Dictionary<IForgeEntity, IProxy?> _proxies = new(ReferenceEqualityComparer.Instance);
	private readonly Dictionary<ForgeHitbox3D, Proxy3D> _hitboxProxies = [];
	private readonly Dictionary<ForgeHitbox2D, Proxy2D> _hitboxProxies2D = [];
	private readonly List<IProxy> _placed = [];

	// The stand-in placed for a live body, if one is.
	public static bool TryGetStandIn(Rid live, out Rid standIn)
	{
		bool placed = _standIns.TryGetValue(live, out IProxy? proxy);
		standIn = proxy?.StandIn ?? default;
		return placed;
	}

	// Before a query: a body's layer set while it's stood in for, as by what a hit set off, which Godot writes to the
	// live body at once, moves to its stand-in, and the live body is left out again.
	public static void Refresh()
	{
		foreach (IProxy proxy in _standIns.Values)
		{
			proxy.Resync();
		}
	}

	// Each body, and each hitbox of one, where the instigator was shown it at a tick.
	public void Place(IReadOnlyList<RewoundBody> bodies, double shownTick)
	{
		foreach (RewoundBody rewound in bodies)
		{
			if (Find(rewound.Entity) is IProxy proxy)
			{
				proxy.Place(rewound.Body);
				_placed.Add(proxy);
				_standIns[proxy.Live] = proxy;
			}

			if (ForgeHitboxes3D.Of(rewound.Entity) is ForgeHitboxes3D hitboxes)
			{
				PlaceHitboxes(hitboxes, rewound.Body, shownTick);
			}
			else if (ForgeHitboxes2D.Of(rewound.Entity) is ForgeHitboxes2D hitboxes2D)
			{
				PlaceHitboxes(hitboxes2D, rewound.Body, shownTick);
			}
		}
	}

	public void Restore()
	{
		foreach (IProxy proxy in _placed)
		{
			PutBack(proxy);
		}

		_placed.Clear();
	}

	// An entity freed while its stand-in is placed, as by what a hit on it set off, gets its body back first.
	public void Forget(IForgeEntity entity)
	{
		if (!_proxies.Remove(entity, out IProxy? proxy) || proxy is null)
		{
			return;
		}

		if (_placed.Remove(proxy))
		{
			PutBack(proxy);
		}

		proxy.Free();
	}

	public void Clear()
	{
		Restore();

		foreach (IProxy? proxy in _proxies.Values)
		{
			proxy?.Free();
		}

		foreach (Proxy3D proxy in _hitboxProxies.Values)
		{
			proxy.Free();
		}

		foreach (Proxy2D proxy in _hitboxProxies2D.Values)
		{
			proxy.Free();
		}

		_proxies.Clear();
		_hitboxProxies.Clear();
		_hitboxProxies2D.Clear();
	}

	private static void PutBack(IProxy proxy)
	{
		proxy.Restore();
		_standIns.Remove(proxy.Live);
	}

	// An entity on something other than a physics body has nothing to stand in for: its proxy is null.
	private IProxy? Find(IForgeEntity entity)
	{
		if (_proxies.TryGetValue(entity, out IProxy? proxy))
		{
			return proxy;
		}

		if (ForgeEntityBridge.TryGetSpatialNode3D(entity, out Node3D? node3D) && node3D is PhysicsBody3D body3D)
		{
			proxy = new Proxy3D(body3D);
		}
		else if (ForgeEntityBridge.TryGetSpatialNode2D(entity, out Node2D? node2D) && node2D is PhysicsBody2D body2D)
		{
			proxy = new Proxy2D(body2D);
		}

		_proxies[entity] = proxy;
		return proxy;
	}

	// A hitbox stands where it stood against its body then, from the body as the instigator was shown it.
	private void PlaceHitboxes(ForgeHitboxes3D hitboxes, in BodyState body, double shownTick)
	{
		if (hitboxes.Body is not Node3D live)
		{
			return;
		}

		var shownBody = new Transform3D(
			new Basis(Proxy3D.ToGodot(body.Rotation)) * Basis.FromScale(live.GlobalTransform.Basis.Scale),
			Proxy3D.ToGodot(body.Position));

		for (int i = 0; i < hitboxes.Hitboxes.Count; i++)
		{
			ForgeHitbox3D hitbox = hitboxes.Hitboxes[i];

			if (!GodotObject.IsInstanceValid(hitbox) || !hitboxes.TryLocalAt(i, shownTick, out Transform3D local))
			{
				continue;
			}

			if (!_hitboxProxies.TryGetValue(hitbox, out Proxy3D? proxy))
			{
				proxy = new Proxy3D(hitbox);
				_hitboxProxies[hitbox] = proxy;
			}

			proxy.PlaceAt(shownBody * local);
			_placed.Add(proxy);
			_standIns[proxy.Live] = proxy;
		}
	}

	private void PlaceHitboxes(ForgeHitboxes2D hitboxes, in BodyState body, double shownTick)
	{
		if (hitboxes.Body is not Node2D live)
		{
			return;
		}

		var shownBody = new Transform2D(
			Proxy2D.AngleOf(body.Rotation),
			live.GlobalScale,
			live.GlobalSkew,
			new Vector2(body.Position.X, body.Position.Y));

		for (int i = 0; i < hitboxes.Hitboxes.Count; i++)
		{
			ForgeHitbox2D hitbox = hitboxes.Hitboxes[i];

			if (!GodotObject.IsInstanceValid(hitbox) || !hitboxes.TryLocalAt(i, shownTick, out Transform2D local))
			{
				continue;
			}

			if (!_hitboxProxies2D.TryGetValue(hitbox, out Proxy2D? proxy))
			{
				proxy = new Proxy2D(hitbox);
				_hitboxProxies2D[hitbox] = proxy;
			}

			proxy.PlaceAt(shownBody * local);
			_placed.Add(proxy);
			_standIns[proxy.Live] = proxy;
		}
	}

	private interface IProxy
	{
		Rid Live { get; }

		Rid StandIn { get; }

		void Place(in BodyState body);

		void Resync();

		void Restore();

		void Free();
	}

	private sealed class Proxy3D : IProxy
	{
		private readonly PhysicsBody3D _live;

		public Rid Live { get; }

		public Rid StandIn { get; }

		public Proxy3D(PhysicsBody3D live)
		{
			_live = live;
			Live = live.GetRid();
			StandIn = PhysicsServer3D.BodyCreate();
			PhysicsServer3D.BodySetMode(StandIn, PhysicsServer3D.BodyMode.Static);
			PhysicsServer3D.BodySetSpace(StandIn, PhysicsServer3D.BodyGetSpace(Live));
			PhysicsServer3D.BodySetCollisionLayer(StandIn, 0);
			PhysicsServer3D.BodySetCollisionMask(StandIn, 0);
			PhysicsServer3D.BodyAttachObjectInstanceId(StandIn, live.GetInstanceId());
		}

		// The shapes are copied as they are now, as a crouch or a shape turned off leaves them, and the body's scale
		// with them.
		public void Place(in BodyState body)
		{
			Basis basis = new Basis(ToGodot(body.Rotation)) * Basis.FromScale(_live.GlobalTransform.Basis.Scale);
			PlaceAt(new Transform3D(basis, ToGodot(body.Position)));
		}

		public void PlaceAt(Transform3D transform)
		{
			PhysicsServer3D.BodyClearShapes(StandIn);

			foreach (Variant shapeOwner in _live.GetShapeOwners())
			{
				uint owner = shapeOwner.AsUInt32();

				if (_live.IsShapeOwnerDisabled(owner))
				{
					continue;
				}

				Transform3D shapeTransform = _live.ShapeOwnerGetTransform(owner);

				for (int i = 0; i < _live.ShapeOwnerGetShapeCount(owner); i++)
				{
					PhysicsServer3D.BodyAddShape(StandIn, _live.ShapeOwnerGetShape(owner, i).GetRid(), shapeTransform);
				}
			}

			PhysicsServer3D.BodySetCollisionLayer(Live, 0);
			PhysicsServer3D.BodySetState(StandIn, PhysicsServer3D.BodyState.Transform, transform);
			PhysicsServer3D.BodySetCollisionLayer(StandIn, _live.CollisionLayer);
		}

		public void Resync()
		{
			// A layer of 0, as a body that died just now gets, takes its stand-in out too.
			if (GodotObject.IsInstanceValid(_live))
			{
				PhysicsServer3D.BodySetCollisionLayer(Live, 0);
				PhysicsServer3D.BodySetCollisionLayer(StandIn, _live.CollisionLayer);
			}
		}

		// The node's layer, which the physics server's change didn't touch, is the live one, as what the hit set off
		// left it.
		public void Restore()
		{
			PhysicsServer3D.BodySetCollisionLayer(StandIn, 0);

			if (GodotObject.IsInstanceValid(_live))
			{
				PhysicsServer3D.BodySetCollisionLayer(Live, _live.CollisionLayer);
			}
		}

		public void Free()
		{
			PhysicsServer3D.FreeRid(StandIn);
		}

		internal static Vector3 ToGodot(NumericsVector3 value)
		{
			return new Vector3(value.X, value.Y, value.Z);
		}

		internal static Quaternion ToGodot(NumericsQuaternion value)
		{
			return new Quaternion(value.X, value.Y, value.Z, value.W).Normalized();
		}
	}

	// A 2D body's state keeps its place in X and Y, and its turn about Z.
	private sealed class Proxy2D : IProxy
	{
		private readonly PhysicsBody2D _live;

		public Rid Live { get; }

		public Rid StandIn { get; }

		public Proxy2D(PhysicsBody2D live)
		{
			_live = live;
			Live = live.GetRid();
			StandIn = PhysicsServer2D.BodyCreate();
			PhysicsServer2D.BodySetMode(StandIn, PhysicsServer2D.BodyMode.Static);
			PhysicsServer2D.BodySetSpace(StandIn, PhysicsServer2D.BodyGetSpace(Live));
			PhysicsServer2D.BodySetCollisionLayer(StandIn, 0);
			PhysicsServer2D.BodySetCollisionMask(StandIn, 0);
			PhysicsServer2D.BodyAttachObjectInstanceId(StandIn, live.GetInstanceId());
		}

		public static float AngleOf(NumericsQuaternion turn)
		{
			return 2 * Mathf.Atan2(turn.Z, turn.W);
		}

		public void Place(in BodyState body)
		{
			PlaceAt(new Transform2D(
				AngleOf(body.Rotation),
				_live.GlobalScale,
				_live.GlobalSkew,
				new Vector2(body.Position.X, body.Position.Y)));
		}

		public void PlaceAt(Transform2D transform)
		{
			PhysicsServer2D.BodyClearShapes(StandIn);

			foreach (Variant shapeOwner in _live.GetShapeOwners())
			{
				uint owner = shapeOwner.AsUInt32();

				if (_live.IsShapeOwnerDisabled(owner))
				{
					continue;
				}

				Transform2D shapeTransform = _live.ShapeOwnerGetTransform(owner);

				for (int i = 0; i < _live.ShapeOwnerGetShapeCount(owner); i++)
				{
					PhysicsServer2D.BodyAddShape(StandIn, _live.ShapeOwnerGetShape(owner, i).GetRid(), shapeTransform);
				}
			}

			PhysicsServer2D.BodySetCollisionLayer(Live, 0);
			PhysicsServer2D.BodySetState(StandIn, PhysicsServer2D.BodyState.Transform, transform);
			PhysicsServer2D.BodySetCollisionLayer(StandIn, _live.CollisionLayer);
		}

		public void Resync()
		{
			// A layer of 0, as a body that died just now gets, takes its stand-in out too.
			if (GodotObject.IsInstanceValid(_live))
			{
				PhysicsServer2D.BodySetCollisionLayer(Live, 0);
				PhysicsServer2D.BodySetCollisionLayer(StandIn, _live.CollisionLayer);
			}
		}

		public void Restore()
		{
			PhysicsServer2D.BodySetCollisionLayer(StandIn, 0);

			if (GodotObject.IsInstanceValid(_live))
			{
				PhysicsServer2D.BodySetCollisionLayer(Live, _live.CollisionLayer);
			}
		}

		public void Free()
		{
			PhysicsServer2D.FreeRid(StandIn);
		}
	}
}
