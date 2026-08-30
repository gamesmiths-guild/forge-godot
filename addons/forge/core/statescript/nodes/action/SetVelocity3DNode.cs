// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript;
using Godot;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;

/// <summary>
/// Action node that sets the velocity of the body an entity lives on.
/// </summary>
/// <remarks>
/// <para>Writes <see cref="CharacterBody3D.Velocity"/> or <see cref="RigidBody3D.LinearVelocity"/>, whichever the body
/// is; anything else has no velocity to set and is skipped. This is the dash and knockback primitive: aim the entity
/// input at whoever should move, and the velocity input at where they should go.</para>
/// <para>A character body only travels if the game moves it. Godot's <see cref="CharacterBody3D"/> stores velocity but
/// does not act on it until something calls <see cref="CharacterBody3D.MoveAndSlide"/>, which stays the game's job
/// exactly as it is for player input.</para>
/// </remarks>
/// <param name="nodePath">Optional path to the body to write, for an entity whose body is not its own spatial node. A
/// path that resolves to nothing falls back to that spatial node, since velocity has nowhere else to live.</param>
[StatescriptCategory("Physics")]
public sealed class SetVelocity3DNode(string nodePath = "") : SpatialActionNodeBase3D(nodePath)
{
	/// <summary>
	/// Input property index for the velocity, in units per second.
	/// </summary>
	public const byte VelocityInput = 1;

	/// <inheritdoc/>
	public override string Description => "Sets the velocity of the body an entity lives on.";

	/// <inheritdoc/>
	protected override bool FallsBackToEntityNode => true;

	/// <inheritdoc/>
	protected override void DefineSpatialParameters(
		List<InputProperty> inputProperties,
		List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Velocity", typeof(NumericsVector3)));
	}

	/// <inheritdoc/>
	protected override void ExecuteOn(Node3D spatialNode, GraphContext graphContext)
	{
		if (!graphContext.TryResolve(InputProperties[VelocityInput].BoundName, out NumericsVector3 velocity))
		{
			return;
		}

		var value = new Vector3(velocity.X, velocity.Y, velocity.Z);

		switch (spatialNode)
		{
			case CharacterBody3D characterBody:
				characterBody.Velocity = value;
				break;

			case RigidBody3D rigidBody:
				rigidBody.LinearVelocity = value;
				break;

			default:
				ReportUnusableNodeOnce(
					$"resolved to a {spatialNode.GetType().Name}, which has no velocity to set. Point Node at the" +
					" body, or leave it empty. The write was skipped.");
				return;
		}

		PhysicsDebugDraw3D.FlashArrow(
			graphContext,
			spatialNode.GlobalPosition,
			value,
			PhysicsDebugDraw3D.ForceColor);
	}
}
