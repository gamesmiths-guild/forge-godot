// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript;
using Godot;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;

/// <summary>
/// Action node that applies an impulse to the rigid body an entity lives on.
/// </summary>
/// <remarks>
/// An impulse is a one-off push the simulation then damps and redirects, which is what separates this from Set Velocity
/// 3D: use this for physics props and ragdolls that should tumble, and Set Velocity 3D for a character whose motion the
/// game controls. Anything that is not a <see cref="RigidBody3D"/> has no impulse to receive and is skipped.
/// </remarks>
/// <param name="nodePath">Optional path to the body to push, for an entity whose body is not its own spatial node. A
/// path that resolves to nothing falls back to that spatial node, since an impulse has nowhere else to land.</param>
[StatescriptCategory("Physics")]
public sealed class ApplyImpulse3DNode(string nodePath = "") : SpatialActionNodeBase3D(nodePath)
{
	/// <summary>
	/// Input property index for the impulse.
	/// </summary>
	public const byte ImpulseInput = 1;

	/// <summary>
	/// Input property index for the optional offset from the body's centre of mass to push at. Unbound pushes through
	/// the centre, which moves the body without spinning it.
	/// </summary>
	public const byte AtOffsetInput = 2;

	/// <inheritdoc/>
	public override string Description => "Applies an impulse to the rigid body an entity lives on.";

	/// <inheritdoc/>
	protected override bool FallsBackToEntityNode => true;

	/// <inheritdoc/>
	protected override void DefineSpatialParameters(
		List<InputProperty> inputProperties,
		List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Impulse", typeof(NumericsVector3)));
		inputProperties.Add(new InputProperty("At Offset", typeof(NumericsVector3), IsOptional: true));
	}

	/// <inheritdoc/>
	protected override void ExecuteOn(Node3D spatialNode, GraphContext graphContext)
	{
		if (spatialNode is not RigidBody3D rigidBody)
		{
			ReportUnusableNodeOnce(
				$"resolved to a {spatialNode.GetType().Name}, which takes no impulse. Only a RigidBody3D does. The" +
				" write was skipped.");

			return;
		}

		if (!graphContext.TryResolve(InputProperties[ImpulseInput].BoundName, out NumericsVector3 impulse))
		{
			return;
		}

		graphContext.TryResolve(InputProperties[AtOffsetInput].BoundName, out NumericsVector3 offset);

		var force = new Vector3(impulse.X, impulse.Y, impulse.Z);
		var at = new Vector3(offset.X, offset.Y, offset.Z);

		rigidBody.ApplyImpulse(force, at);

		PhysicsDebugDraw3D.FlashArrow(
			graphContext,
			spatialNode.GlobalPosition + at,
			force,
			PhysicsDebugDraw3D.ForceColor);
	}
}
