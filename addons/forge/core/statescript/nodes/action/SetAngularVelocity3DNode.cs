// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript;
using Godot;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;

/// <summary>
/// Action node that sets how fast the body an entity lives on is spinning.
/// </summary>
/// <remarks>
/// <para>The angular counterpart of Set Velocity 3D: a spin started outright rather than built up by a torque. Sending
/// a hit target tumbling, putting a thrown object into a controlled rotation, stopping a spin dead by writing zero.
/// </para>
/// <para>Only a <see cref="RigidBody3D"/> has an angular velocity to write. A <see cref="CharacterBody3D"/> is turned
/// by the game rather than by physics, so it is reported rather than silently skipped - Set Rotation 3D and Rotate To
/// 3D are what turn one of those.</para>
/// <para>The vector is an axis with the rate as its length, so core's Scale over a normalized axis is how a spin about
/// something specific is authored.</para>
/// </remarks>
/// <param name="nodePath">Optional path to the body to write, for an entity whose body is not its own spatial node. A
/// path that resolves to nothing falls back to that spatial node, since a spin has nowhere else to live.</param>
[StatescriptCategory("Physics")]
public sealed class SetAngularVelocity3DNode(string nodePath = "") : SpatialActionNodeBase3D(nodePath)
{
	/// <summary>
	/// Input property index for the angular velocity, in radians per second about the vector's own axis.
	/// </summary>
	public const byte AngularVelocityInput = 1;

	/// <inheritdoc/>
	public override string Description => "Sets how fast the body an entity lives on is spinning.";

	/// <inheritdoc/>
	protected override bool FallsBackToEntityNode => true;

	/// <inheritdoc/>
	protected override void DefineSpatialParameters(
		List<InputProperty> inputProperties,
		List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Angular Velocity", typeof(NumericsVector3)));
	}

	/// <inheritdoc/>
	protected override void ExecuteOn(Node3D spatialNode, GraphContext graphContext)
	{
		if (!graphContext.TryResolve(
			InputProperties[AngularVelocityInput].BoundName,
			out NumericsVector3 angularVelocity))
		{
			return;
		}

		if (spatialNode is not RigidBody3D rigidBody)
		{
			ReportUnusableNodeOnce(
				$"resolved to a {spatialNode.GetType().Name}, which has no angular velocity to set - only a" +
				" RigidBody3D does. Use Set Rotation 3D or Rotate To 3D to turn anything else. The write was" +
				" skipped.");

			return;
		}

		var value = new Vector3(angularVelocity.X, angularVelocity.Y, angularVelocity.Z);
		rigidBody.AngularVelocity = value;

		PhysicsDebugDraw3D.FlashArrow(
			graphContext,
			rigidBody.GlobalPosition,
			value,
			PhysicsDebugDraw3D.ForceColor);
	}
}
