// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Networking.Replication;
using Godot;
using NumericsQuaternion = System.Numerics.Quaternion;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core;

// Stands bodies where an instigator was shown them, for the queries of a lag-compensated hit. Godot moves a kinematic
// body only on its next physics step, so the live bodies can't be moved back; each gets a static body of its own in the
// physics server instead, made of the same shapes, which takes the live body's layers while the live one has none.
// A hit on it reports the live node, whose instance it carries, so whatever reads the hit finds the entity as ever.
internal sealed class LagCompensationProxies
{
	private readonly Dictionary<IForgeEntity, IProxy?> _proxies = new(ReferenceEqualityComparer.Instance);
	private readonly List<IProxy> _placed = [];

	public void Place(IReadOnlyList<RewoundBody> bodies)
	{
		foreach (RewoundBody rewound in bodies)
		{
			if (Find(rewound.Entity) is IProxy proxy)
			{
				proxy.Place(rewound.Body);
				_placed.Add(proxy);
			}
		}
	}

	public void Restore()
	{
		foreach (IProxy proxy in _placed)
		{
			proxy.Restore();
		}

		_placed.Clear();
	}

	public void Forget(IForgeEntity entity)
	{
		if (_proxies.Remove(entity, out IProxy? proxy))
		{
			proxy?.Free();
		}
	}

	public void Clear()
	{
		Restore();

		foreach (IProxy? proxy in _proxies.Values)
		{
			proxy?.Free();
		}

		_proxies.Clear();
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

	private interface IProxy
	{
		void Place(in BodyState body);

		void Restore();

		void Free();
	}

	private sealed class Proxy3D : IProxy
	{
		private readonly PhysicsBody3D _live;
		private readonly Rid _rid;
		private uint _layer;

		public Proxy3D(PhysicsBody3D live)
		{
			_live = live;
			_rid = PhysicsServer3D.BodyCreate();
			PhysicsServer3D.BodySetMode(_rid, PhysicsServer3D.BodyMode.Static);
			PhysicsServer3D.BodySetSpace(_rid, PhysicsServer3D.BodyGetSpace(live.GetRid()));
			PhysicsServer3D.BodySetCollisionLayer(_rid, 0);
			PhysicsServer3D.BodySetCollisionMask(_rid, 0);
			PhysicsServer3D.BodyAttachObjectInstanceId(_rid, live.GetInstanceId());

			foreach (Variant shapeOwner in live.GetShapeOwners())
			{
				uint owner = shapeOwner.AsUInt32();

				if (live.IsShapeOwnerDisabled(owner))
				{
					continue;
				}

				Transform3D transform = live.ShapeOwnerGetTransform(owner);

				for (int i = 0; i < live.ShapeOwnerGetShapeCount(owner); i++)
				{
					PhysicsServer3D.BodyAddShape(_rid, live.ShapeOwnerGetShape(owner, i).GetRid(), transform);
				}
			}
		}

		public void Place(in BodyState body)
		{
			_layer = PhysicsServer3D.BodyGetCollisionLayer(_live.GetRid());
			PhysicsServer3D.BodySetCollisionLayer(_live.GetRid(), 0);
			PhysicsServer3D.BodySetState(
				_rid,
				PhysicsServer3D.BodyState.Transform,
				new Transform3D(new Basis(ToGodot(body.Rotation)), ToGodot(body.Position)));
			PhysicsServer3D.BodySetCollisionLayer(_rid, _layer);
		}

		public void Restore()
		{
			PhysicsServer3D.BodySetCollisionLayer(_rid, 0);

			if (GodotObject.IsInstanceValid(_live))
			{
				PhysicsServer3D.BodySetCollisionLayer(_live.GetRid(), _layer);
			}
		}

		public void Free()
		{
			PhysicsServer3D.FreeRid(_rid);
		}

		private static Vector3 ToGodot(NumericsVector3 value)
		{
			return new Vector3(value.X, value.Y, value.Z);
		}

		private static Quaternion ToGodot(NumericsQuaternion value)
		{
			return new Quaternion(value.X, value.Y, value.Z, value.W).Normalized();
		}
	}

	// A 2D body's state keeps its place in X and Y, and its turn about Z.
	private sealed class Proxy2D : IProxy
	{
		private readonly PhysicsBody2D _live;
		private readonly Rid _rid;
		private uint _layer;

		public Proxy2D(PhysicsBody2D live)
		{
			_live = live;
			_rid = PhysicsServer2D.BodyCreate();
			PhysicsServer2D.BodySetMode(_rid, PhysicsServer2D.BodyMode.Static);
			PhysicsServer2D.BodySetSpace(_rid, PhysicsServer2D.BodyGetSpace(live.GetRid()));
			PhysicsServer2D.BodySetCollisionLayer(_rid, 0);
			PhysicsServer2D.BodySetCollisionMask(_rid, 0);
			PhysicsServer2D.BodyAttachObjectInstanceId(_rid, live.GetInstanceId());

			foreach (Variant shapeOwner in live.GetShapeOwners())
			{
				uint owner = shapeOwner.AsUInt32();

				if (live.IsShapeOwnerDisabled(owner))
				{
					continue;
				}

				Transform2D transform = live.ShapeOwnerGetTransform(owner);

				for (int i = 0; i < live.ShapeOwnerGetShapeCount(owner); i++)
				{
					PhysicsServer2D.BodyAddShape(_rid, live.ShapeOwnerGetShape(owner, i).GetRid(), transform);
				}
			}
		}

		public void Place(in BodyState body)
		{
			_layer = PhysicsServer2D.BodyGetCollisionLayer(_live.GetRid());
			PhysicsServer2D.BodySetCollisionLayer(_live.GetRid(), 0);
			NumericsQuaternion turn = body.Rotation;
			float angle = 2 * Mathf.Atan2(turn.Z, turn.W);
			PhysicsServer2D.BodySetState(
				_rid,
				PhysicsServer2D.BodyState.Transform,
				new Transform2D(angle, new Vector2(body.Position.X, body.Position.Y)));
			PhysicsServer2D.BodySetCollisionLayer(_rid, _layer);
		}

		public void Restore()
		{
			PhysicsServer2D.BodySetCollisionLayer(_rid, 0);

			if (GodotObject.IsInstanceValid(_live))
			{
				PhysicsServer2D.BodySetCollisionLayer(_live.GetRid(), _layer);
			}
		}

		public void Free()
		{
			PhysicsServer2D.FreeRid(_rid);
		}
	}
}
