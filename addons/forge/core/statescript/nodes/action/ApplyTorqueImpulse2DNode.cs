// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;

/// <summary>
/// Action node that applies a torque impulse to the rigid body an entity lives on.
/// </summary>
/// <remarks>
/// <para>The angular counterpart of Apply Impulse 2D: a kick that spins rather than shoves. A spin attack that sends
/// a target tumbling, a thrown object given a wobble, a vehicle knocked into a skid.</para>
/// <para>The torque is a <b>number</b> rather than an axis vector, because a plane has one axis to turn around; its
/// sign is which way round the kick goes. There is no offset input, unlike its linear twin: an offset is what turns a
/// push into a spin, and this is already the spin.</para>
/// <para>Only a <see cref="RigidBody2D"/> takes one.</para>
/// </remarks>
/// <param name="nodePath">Optional path to the body to write, for an entity whose body is not its own spatial node. A
/// path that resolves to nothing falls back to that spatial node, since a torque has nowhere else to land.</param>
[StatescriptCategory("Physics")]
public sealed class ApplyTorqueImpulse2DNode(string nodePath = "") : SpatialActionNodeBase2D(nodePath)
{
	/// <summary>
	/// Input property index for the torque impulse.
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
		inputProperties.Add(new InputProperty("Torque", typeof(double)));
	}

	/// <inheritdoc/>
	protected override void ExecuteOn(Node2D spatialNode, GraphContext graphContext)
	{
		if (spatialNode is not RigidBody2D rigidBody)
		{
			ReportUnusableNodeOnce(
				$"resolved to a {spatialNode.GetType().Name}, which takes no torque. Only a RigidBody2D does. The" +
				" write was skipped.");

			return;
		}

		if (!graphContext.TryResolve(InputProperties[TorqueInput].BoundName, out double torque))
		{
			return;
		}

		rigidBody.ApplyTorqueImpulse((float)torque);
	}
}
