// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using GodotRidArray = Godot.Collections.Array<Godot.Rid>;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Condition;

/// <summary>
/// Condition node that casts a ray, routing True when it hits something and False when it does not.
/// </summary>
/// <remarks>
/// <para>It is a Condition rather than an Action because a ray is asked in order to branch on the answer: hitscan
/// shots, line-of-sight gates and floor probes all continue differently depending on whether anything was there.
/// Making it a Condition spares every one of those graphs an Action followed by an expression that only exists to test
/// what the Action already knew.</para>
/// <para>All five outputs are written before either port fires, including on a miss, so nothing downstream can read a
/// position that disagrees with the entity beside it or a hit left over from a previous cast.</para>
/// <para>The ray does not exclude the caster. Aim it from a muzzle that sits outside the caster's own collider, or
/// narrow the mask, exactly as the equivalent Godot code would.</para>
/// </remarks>
/// <param name="collideWithAreas">Whether areas count as hits, as well as bodies.</param>
/// <param name="hitFromInside">Whether a ray starting inside a shape reports that shape.</param>
[StatescriptCategory("Physics")]
public sealed class Raycast2DNode(bool collideWithAreas = false, bool hitFromInside = false) : ConditionNode
{
	private readonly bool _collideWithAreas = collideWithAreas;
	private readonly bool _hitFromInside = hitFromInside;
	private readonly GodotRidArray _exclusions = [];

	/// <inheritdoc/>
	public override string Description => "Casts a ray, routing on whether it hit and writing what it found.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		RaycastNodeParameters2D.Define(inputProperties, outputVariables);
	}

	/// <inheritdoc/>
	protected override bool Test(GraphContext graphContext)
	{
		bool hit = RaycastNodeParameters2D.TryCast(
			graphContext,
			InputProperties,
			_collideWithAreas,
			_hitFromInside,
			_exclusions,
			out RaycastResult2D result,
			out RaySegment2D segment);

		RaycastNodeParameters2D.WriteOutputs(graphContext, OutputVariables, result);

		PhysicsDebugDraw2D.FlashLine(
			graphContext,
			segment.From,
			segment.To,
			hit ? PhysicsDebugDraw2D.RayHitColor : PhysicsDebugDraw2D.RayClearColor);

		return hit;
	}
}
