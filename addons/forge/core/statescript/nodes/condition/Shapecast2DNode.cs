// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Godot;
using GodotRidArray = Godot.Collections.Array<Godot.Rid>;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Condition;

/// <summary>
/// Condition node that sweeps a shape through the world, routing True when it meets something and False when it does
/// not.
/// </summary>
/// <remarks>
/// <para>Raycast 2D with thickness. A ray is a line with no width, so a fast projectile aimed at a thin target slips
/// past it and a dash checked with a ray walks a character's shoulders into a wall; sweeping the volume that is
/// actually moving gives the check the width the thing being checked has.</para>
/// <para>It is a Condition for the same reason the ray is: a sweep is asked in order to branch on the answer. Dash
/// if the path is clear, stop short if it is not.</para>
/// <para>The Shapecast 2D <em>resolver</em> answers the same question with one value, the entity. This node exists
/// because a sweep reports five things that have to agree with each other - where it stopped, the surface normal
/// there, what stopped it, and how far it got - and a resolver can hand back only one of them.</para>
/// <para>All five outputs are written before either port fires, including on a miss, so nothing downstream can read a
/// position that disagrees with the entity beside it or a hit left over from a previous sweep.</para>
/// </remarks>
/// <param name="collideWithAreas">Whether areas stop the sweep, as well as bodies.</param>
[StatescriptCategory("Physics")]
public sealed class Shapecast2DNode(bool collideWithAreas = false) : ConditionNode
{
	private readonly bool _collideWithAreas = collideWithAreas;
	private readonly GodotRidArray _exclusions = [];

	/// <inheritdoc/>
	public override string Description =>
		"Sweeps a shape through the world, routing on whether it met anything and writing what it found.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		ShapecastNodeParameters2D.Define(inputProperties, outputVariables);
	}

	/// <inheritdoc/>
	protected override bool Test(GraphContext graphContext)
	{
		bool hit = ShapecastNodeParameters2D.TryCast(
			graphContext,
			InputProperties,
			_collideWithAreas,
			_exclusions,
			out Shape2D? shape,
			out RaycastResult2D result,
			out Transform2D hitTransform,
			out RaySegment2D segment);

		ShapecastNodeParameters2D.WriteOutputs(graphContext, OutputVariables, result);

		if (shape is not null)
		{
			PhysicsDebugDraw2D.FlashShapecast(
				graphContext,
				shape,
				hitTransform,
				segment.From,
				segment.To,
				hit ? PhysicsDebugDraw2D.RayHitColor : PhysicsDebugDraw2D.RayClearColor);
		}

		PhysicsDebugDraw2D.FlashTarget(graphContext, result.Entity, PhysicsDebugDraw2D.RayHitColor);

		return hit;
	}
}
