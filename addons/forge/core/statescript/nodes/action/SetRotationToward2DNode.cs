// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Statescript;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;

/// <summary>
/// Action node that turns an entity to face a point.
/// </summary>
/// <remarks>
/// Unlike its 3D twin there is nothing to flatten: a plane has one axis to turn around, so a look-at is always the turn
/// the 3D node has to be told to restrict itself to. The node faces the point with its +X axis, which is what Godot
/// treats as a 2D node's forward.
/// </remarks>
/// <param name="nodePath">Optional path to a descendant node to turn instead of the entity's own spatial node.</param>
[StatescriptCategory("Spatial")]
public sealed class SetRotationToward2DNode(string nodePath = "") : SpatialActionNodeBase2D(nodePath)
{
	/// <summary>
	/// Input property index for the point to face.
	/// </summary>
	public const byte TargetInput = 1;

	/// <inheritdoc/>
	public override string Description => "Turns an entity to face a point.";

	/// <inheritdoc/>
	protected override void DefineSpatialParameters(
		List<InputProperty> inputProperties,
		List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Target", typeof(NumericsVector2)));
	}

	/// <inheritdoc/>
	protected override void ExecuteOn(Node2D spatialNode, GraphContext graphContext)
	{
		if (!graphContext.TryResolve(InputProperties[TargetInput].BoundName, out NumericsVector2 target))
		{
			return;
		}

		var point = new Vector2(target.X, target.Y);

		// A target standing exactly on the caster names no direction, and turning to a number that came out of a zero
		// vector would snap the node to whatever angle that rounds to.
		if (point.DistanceSquaredTo(spatialNode.GlobalPosition) <= 0.000001f)
		{
			return;
		}

		spatialNode.LookAt(point);
	}
}
