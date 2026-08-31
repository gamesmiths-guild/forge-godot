// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;

/// <summary>
/// Action node that applies an impulse to the rigid body an entity lives on.
/// </summary>
/// <remarks>
/// An impulse is a one-off push the simulation then damps and redirects, which is what separates this from Set Velocity
/// 2D: use this for physics props and ragdolls that should tumble, and Set Velocity 2D for a character whose motion the
/// game controls. Anything that is not a <see cref="RigidBody2D"/> has no impulse to receive and is skipped.
/// </remarks>
/// <param name="nodePath">Optional path to the body to push, for an entity whose body is not its own spatial node. A
/// path that resolves to nothing falls back to that spatial node, since an impulse has nowhere else to land.</param>
[StatescriptCategory("Physics")]
public sealed class ApplyImpulse2DNode(string nodePath = "") : SpatialActionNodeBase2D(nodePath)
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
		inputProperties.Add(new InputProperty("Impulse", typeof(NumericsVector2)));
		inputProperties.Add(new InputProperty("At Offset", typeof(NumericsVector2), IsOptional: true));
	}

	/// <inheritdoc/>
	protected override void ExecuteOn(Node2D spatialNode, GraphContext graphContext)
	{
		if (spatialNode is not RigidBody2D rigidBody)
		{
			ReportUnusableNodeOnce(
				$"resolved to a {spatialNode.GetType().Name}, which takes no impulse. Only a RigidBody2D does. The" +
				" write was skipped.");

			return;
		}

		if (!graphContext.TryResolve(InputProperties[ImpulseInput].BoundName, out NumericsVector2 impulse))
		{
			return;
		}

		graphContext.TryResolve(InputProperties[AtOffsetInput].BoundName, out NumericsVector2 offset);

		var force = new Vector2(impulse.X, impulse.Y);
		var at = new Vector2(offset.X, offset.Y);

		rigidBody.ApplyImpulse(force, at);

		PhysicsDebugDraw2D.FlashArrow(
			graphContext,
			spatialNode.GlobalPosition + at,
			force,
			PhysicsDebugDraw2D.ForceColor);
	}
}
