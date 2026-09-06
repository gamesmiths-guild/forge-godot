// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Statescript;
using Godot;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;

/// <summary>
/// Action node that turns an entity to face a point.
/// </summary>
/// <remarks>
/// Flattening is on by default because the usual intent is "turn towards them", and a character that pitches to look at
/// a target's feet or a point overhead reads as a bug. Turn it off for anything that genuinely aims in three
/// dimensions, such as a turret.
/// </remarks>
/// <param name="flatten">Whether to ignore the height difference and turn only around the vertical axis.</param>
/// <param name="nodePath">Optional path to a descendant node to turn instead of the entity's own spatial node.</param>
[StatescriptCategory("Spatial")]
public sealed class SetRotationToward3DNode(bool flatten = true, string nodePath = "")
	: SpatialActionNodeBase3D(nodePath)
{
	/// <summary>
	/// Input property index for the point to face.
	/// </summary>
	public const byte TargetInput = 1;

	private readonly bool _flatten = flatten;

	/// <inheritdoc/>
	public override string Description => "Turns an entity to face a point.";

	/// <inheritdoc/>
	protected override void DefineSpatialParameters(
		List<InputProperty> inputProperties,
		List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Target", typeof(NumericsVector3)));
	}

	/// <inheritdoc/>
	protected override void ExecuteOn(Node3D spatialNode, GraphContext graphContext)
	{
		if (!graphContext.TryResolve(InputProperties[TargetInput].BoundName, out NumericsVector3 target))
		{
			return;
		}

		var point = new Vector3(target.X, target.Y, target.Z);

		if (_flatten)
		{
			point.Y = spatialNode.GlobalPosition.Y;
		}

		// LookAt throws when the target coincides with the node, or when the direction is parallel to up. Both happen
		// in practice - a target standing exactly on the caster, or a flattened look at someone directly overhead.
		Vector3 offset = point - spatialNode.GlobalPosition;

		if (offset.LengthSquared() <= 0.000001f)
		{
			return;
		}

		if (Mathf.Abs(offset.Normalized().Dot(Vector3.Up)) > 0.9999f)
		{
			return;
		}

		spatialNode.LookAt(point, Vector3.Up);
	}
}
