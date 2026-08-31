// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;

/// <summary>
/// Action node that sets how fast the body an entity lives on is spinning.
/// </summary>
/// <remarks>
/// <para>The angular counterpart of Set Velocity 2D: a spin started outright rather than built up by a torque. Sending
/// a hit target tumbling, putting a thrown object into a controlled rotation, stopping a spin dead by writing zero.
/// </para>
/// <para>The rate is a <b>number</b> in radians per second, not a vector: a plane has one axis to turn around, so its
/// sign is the whole of which way round it goes.</para>
/// <para>Only a <see cref="RigidBody2D"/> has an angular velocity to write. A <see cref="CharacterBody2D"/> is turned
/// by the game rather than by physics, so it is reported rather than silently skipped - Set Rotation 2D and Rotate To
/// 2D are what turn one of those.</para>
/// </remarks>
/// <param name="nodePath">Optional path to the body to write, for an entity whose body is not its own spatial node. A
/// path that resolves to nothing falls back to that spatial node, since a spin has nowhere else to live.</param>
[StatescriptCategory("Physics")]
public sealed class SetAngularVelocity2DNode(string nodePath = "") : SpatialActionNodeBase2D(nodePath)
{
	/// <summary>
	/// Input property index for the angular velocity, in radians per second.
	/// </summary>
	public const byte AngularVelocityInput = 1;

	/// <inheritdoc/>
	public override string Description => "Sets how fast the body an entity lives on is spinning, in radians.";

	/// <inheritdoc/>
	protected override bool FallsBackToEntityNode => true;

	/// <inheritdoc/>
	protected override void DefineSpatialParameters(
		List<InputProperty> inputProperties,
		List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Angular Velocity", typeof(double)));
	}

	/// <inheritdoc/>
	protected override void ExecuteOn(Node2D spatialNode, GraphContext graphContext)
	{
		if (!graphContext.TryResolve(InputProperties[AngularVelocityInput].BoundName, out double angularVelocity))
		{
			return;
		}

		if (spatialNode is not RigidBody2D rigidBody)
		{
			ReportUnusableNodeOnce(
				$"resolved to a {spatialNode.GetType().Name}, which has no angular velocity to set - only a" +
				" RigidBody2D does. Use Set Rotation 2D or Rotate To 2D to turn anything else. The write was" +
				" skipped.");

			return;
		}

		rigidBody.AngularVelocity = (float)angularVelocity;
	}
}
