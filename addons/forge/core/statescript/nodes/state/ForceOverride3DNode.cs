// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Godot;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// State node that holds a constant force and torque on a rigid body for as long as it is active.
/// </summary>
/// <remarks>
/// <para>This is acceleration, which nothing else in the layer expresses. An impulse is a kick that happens once and
/// is over; a velocity write is a speed set outright. A held force is neither — it is a push that keeps pushing, so
/// the body builds speed for as long as the node runs and coasts once it stops. Thrusters, updrafts, tractor beams, a
/// gravity well, a channelled pull.</para>
/// <para><b>It writes the body's constant force rather than calling apply force every update.</b> A constant force is
/// integrated by the engine at its own rate whether or not the node ran that step, so the push is the push the author
/// wrote: it is held from activation, given back on deactivation, and the fixed update only re-reads a bound value so
/// a thrust tied to an attribute tracks it. An applied force is cleared every physics step, so it would have to be
/// re-applied on exactly the steps the engine takes - right on a host that drives the fixed rail, and silently weaker
/// on one that does not.</para>
/// <para>Both inputs are optional and are captured and restored independently, so a node that binds only a force puts
/// only the force back and leaves an authored constant torque alone. Restoring happens on deactivate <em>or abort</em>,
/// the same guarantee Collision Override 3D gives: a cancelled ability cannot leave a body accelerating forever.</para>
/// <para>The permanent counterpart is not a node here. A constant force is an ordinary property, so Set Node Property
/// pointed at <c>constant_force</c> already writes one that outlives the ability.</para>
/// <para>Configuration is captured in field initializers rather than a constructor body, because the base node
/// constructor calls <see cref="DefineParameters"/> before a body would run.</para>
/// </remarks>
/// <param name="nodePath">Optional path to the body to drive, for an entity whose body is not its own spatial node.
/// </param>
[StatescriptCategory("Physics")]
public class ForceOverride3DNode(string nodePath = "") : StateNode<ForceOverride3DNodeContext>
{
	/// <summary>
	/// Input property index for the entity to push. Unbound means the ability's owner.
	/// </summary>
	public const byte EntityInput = 0;

	/// <summary>
	/// Input property index for the force held while the node is active. Unbound leaves the body's own alone.
	/// </summary>
	public const byte ForceInput = 1;

	/// <summary>
	/// Input property index for the torque held while the node is active. Unbound leaves the body's own alone.
	/// </summary>
	public const byte TorqueInput = 2;

	private readonly string _nodePath = nodePath ?? string.Empty;

	private bool _reportedMissingBody;

	/// <inheritdoc/>
	public override string Description =>
		"Holds a constant force and torque on a rigid body while active, restoring them on deactivate or abort.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Entity", typeof(IForgeEntity), IsOptional: true));
		inputProperties.Add(new InputProperty("Force", typeof(NumericsVector3), IsOptional: true));
		inputProperties.Add(new InputProperty("Torque", typeof(NumericsVector3), IsOptional: true));
	}

	/// <inheritdoc/>
	protected override void OnActivate(GraphContext graphContext)
	{
		ForceOverride3DNodeContext nodeContext = graphContext.GetNodeContext<ForceOverride3DNodeContext>(NodeID);
		nodeContext.Body = null;
		nodeContext.WroteForce = false;
		nodeContext.WroteTorque = false;

		IForgeEntity? entity = ResolveEntityOrOwner(graphContext);

		if (!ForgeEntityBridge.TryGetSpatialNode3D(entity, _nodePath, out Node3D? spatialNode)
			|| spatialNode is not RigidBody3D rigidBody)
		{
			ReportMissingBodyOnce();
			return;
		}

		nodeContext.Body = rigidBody;

		// An unbound input does not resolve, which is what tells "hold no torque" apart from "hold a torque of zero" -
		// and the difference decides whether the body's own constant torque is captured and overwritten at all.
		if (graphContext.TryResolve(InputProperties[ForceInput].BoundName, out NumericsVector3 force))
		{
			nodeContext.PreviousForce = rigidBody.ConstantForce;
			nodeContext.WroteForce = true;
			rigidBody.ConstantForce = new Vector3(force.X, force.Y, force.Z);
		}

		if (graphContext.TryResolve(InputProperties[TorqueInput].BoundName, out NumericsVector3 torque))
		{
			nodeContext.PreviousTorque = rigidBody.ConstantTorque;
			nodeContext.WroteTorque = true;
			rigidBody.ConstantTorque = new Vector3(torque.X, torque.Y, torque.Z);
		}

		nodeContext.DebugMarker = PhysicsDebugDraw3D.EnsureMarker(
			graphContext,
			nodeContext.DebugMarker,
			PhysicsDebugDraw3D.ForceColor);
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
		ForceOverride3DNodeContext nodeContext = graphContext.GetNodeContext<ForceOverride3DNodeContext>(NodeID);
		RigidBody3D? body = nodeContext.Body;
		nodeContext.Body = null;

		PhysicsDebugDraw3D.Release(nodeContext.DebugMarker);
		nodeContext.DebugMarker = null;

		if (body is null || !GodotObject.IsInstanceValid(body))
		{
			return;
		}

		// Each half is put back only if this node wrote it, so an ability that pushed without twisting cannot clear a
		// constant torque the scene authored.
		if (nodeContext.WroteForce)
		{
			body.ConstantForce = nodeContext.PreviousForce;
		}

		if (nodeContext.WroteTorque)
		{
			body.ConstantTorque = nodeContext.PreviousTorque;
		}

		nodeContext.WroteForce = false;
		nodeContext.WroteTorque = false;
	}

	/// <inheritdoc/>
	protected override void OnFixedUpdate(double deltaTime, GraphContext graphContext)
	{
		ForceOverride3DNodeContext nodeContext = graphContext.GetNodeContext<ForceOverride3DNodeContext>(NodeID);
		RigidBody3D? body = nodeContext.Body;

		if (body is null || !GodotObject.IsInstanceValid(body))
		{
			return;
		}

		// Re-resolved every update rather than only on activation, so a thrust bound to an attribute or a charge level
		// tracks it. The capture is untouched: what is restored is still what the body carried before the node ran.
		if (nodeContext.WroteForce
			&& graphContext.TryResolve(InputProperties[ForceInput].BoundName, out NumericsVector3 force))
		{
			body.ConstantForce = new Vector3(force.X, force.Y, force.Z);
		}

		if (nodeContext.WroteTorque
			&& graphContext.TryResolve(InputProperties[TorqueInput].BoundName, out NumericsVector3 torque))
		{
			body.ConstantTorque = new Vector3(torque.X, torque.Y, torque.Z);
		}

		// Held rather than flashed, matching the other monitored nodes: a push that lasts should read as one arrow
		// following the body, not as a new marker every frame.
		PhysicsDebugDraw3D.SetArrow(nodeContext.DebugMarker, body.GlobalPosition, body.ConstantForce);
	}

	private IForgeEntity? ResolveEntityOrOwner(GraphContext graphContext)
	{
		StringKey boundName = InputProperties[EntityInput].BoundName;

		if (boundName != StringKey.Empty
			&& graphContext.TryResolveObject(boundName, out IForgeEntity? entity)
			&& entity is not null)
		{
			return entity;
		}

		return graphContext.TryGetActivationContext(out AbilityBehaviorContext? abilityContext)
			? abilityContext.Owner
			: null;
	}

	private void ReportMissingBodyOnce()
	{
		if (_reportedMissingBody)
		{
			return;
		}

		_reportedMissingBody = true;

		GD.PushWarning(
			"Statescript: Force Override 3D found no RigidBody3D for its entity" +
			(_nodePath.Length == 0 ? "." : $" at [{_nodePath}].") +
			" Only a rigid body takes a constant force. Nothing was pushed.");
	}
}
