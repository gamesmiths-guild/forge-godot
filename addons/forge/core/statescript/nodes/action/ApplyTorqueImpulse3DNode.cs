// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript;
using Godot;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;

/// <summary>
/// Action node that applies a torque impulse to the rigid body an entity lives on.
/// </summary>
/// <remarks>
/// <para>The angular counterpart of Apply Impulse 3D: a kick that spins rather than shoves. A spin attack that sends
/// a target tumbling, a thrown object given a wobble, a vehicle knocked into a skid.</para>
/// <para>There is no offset input, unlike its linear twin. An offset is what turns a push into a spin, and this is
/// already the spin — an off-centre torque is not a thing the physics server has.</para>
/// <para>Only a <see cref="RigidBody3D"/> takes one. The arrow drawn for it is the torque axis at the body's own
/// position, which reads as the axis it is being twisted about rather than as a direction it is being pushed.</para>
/// </remarks>
/// <param name="nodePath">Optional path to the body to write, for an entity whose body is not its own spatial node. A
/// path that resolves to nothing falls back to that spatial node, since a torque has nowhere else to land.</param>
[StatescriptCategory("Physics")]
public sealed class ApplyTorqueImpulse3DNode(string nodePath = "") : SpatialActionNodeBase3D(nodePath)
{
	/// <summary>
	/// Input property index for the torque impulse, as an axis with the strength as its length.
	/// </summary>
	public const byte TorqueInput = 1;

	/// <inheritdoc/>
	public override string Description => "Applies a torque impulse to the rigid body an entity lives on.";

	/// <inheritdoc/>
	protected override bool FallsBackToEntityNode => true;

	/// <inheritdoc/>
	protected override void DefineSpatialParameters(
		List<InputProperty> inputProperties,
		List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Torque", typeof(NumericsVector3)));
	}

	/// <inheritdoc/>
	protected override void ExecuteOn(Node3D spatialNode, GraphContext graphContext)
	{
		if (spatialNode is not RigidBody3D rigidBody)
		{
			ReportUnusableNodeOnce(
				$"resolved to a {spatialNode.GetType().Name}, which takes no torque. Only a RigidBody3D does. The" +
				" write was skipped.");

			return;
		}

		if (!graphContext.TryResolve(InputProperties[TorqueInput].BoundName, out NumericsVector3 torque))
		{
			return;
		}

		var value = new Vector3(torque.X, torque.Y, torque.Z);
		rigidBody.ApplyTorqueImpulse(value);

		PhysicsDebugDraw3D.FlashArrow(
			graphContext,
			rigidBody.GlobalPosition,
			value,
			PhysicsDebugDraw3D.ForceColor);
	}
}
