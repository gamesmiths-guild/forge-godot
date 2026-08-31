// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;

/// <summary>
/// Action node that sets the velocity of the body an entity lives on.
/// </summary>
/// <remarks>
/// <para>Writes <see cref="CharacterBody2D.Velocity"/> or <see cref="RigidBody2D.LinearVelocity"/>, whichever the body
/// is; anything else has no velocity to set and is skipped. This is the dash and knockback primitive: aim the entity
/// input at whoever should move, and the velocity input at where they should go.</para>
/// <para>A character body only travels if the game moves it. Godot's <see cref="CharacterBody2D"/> stores velocity but
/// does not act on it until something calls <see cref="CharacterBody2D.MoveAndSlide"/>, which stays the game's job
/// exactly as it is for player input.</para>
/// </remarks>
/// <param name="nodePath">Optional path to the body to write, for an entity whose body is not its own spatial node. A
/// path that resolves to nothing falls back to that spatial node, since velocity has nowhere else to live.</param>
[StatescriptCategory("Physics")]
public sealed class SetVelocity2DNode(string nodePath = "") : SpatialActionNodeBase2D(nodePath)
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
		inputProperties.Add(new InputProperty("Velocity", typeof(NumericsVector2)));
	}

	/// <inheritdoc/>
	protected override void ExecuteOn(Node2D spatialNode, GraphContext graphContext)
	{
		if (!graphContext.TryResolve(InputProperties[VelocityInput].BoundName, out NumericsVector2 velocity))
		{
			return;
		}

		var value = new Vector2(velocity.X, velocity.Y);

		switch (spatialNode)
		{
			case CharacterBody2D characterBody:
				characterBody.Velocity = value;
				break;

			case RigidBody2D rigidBody:
				rigidBody.LinearVelocity = value;
				break;

			default:
				ReportUnusableNodeOnce(
					$"resolved to a {spatialNode.GetType().Name}, which has no velocity to set. Point Node at the" +
					" body, or leave it empty. The write was skipped.");
				return;
		}

		PhysicsDebugDraw2D.FlashArrow(
			graphContext,
			spatialNode.GlobalPosition,
			value,
			PhysicsDebugDraw2D.ForceColor);
	}
}
