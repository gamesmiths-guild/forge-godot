// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Statescript;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;

/// <summary>
/// Action node that sets an entity's scale.
/// </summary>
/// <remarks>
/// Scale is written on the node itself rather than in world space: a global scale assignment on a rotated parent shears
/// the child, which is never what a gameplay graph means by "make it bigger".
/// </remarks>
/// <param name="nodePath">Optional path to a descendant node to scale instead of the entity's own spatial node.</param>
[StatescriptCategory("Spatial")]
public sealed class SetScale2DNode(string nodePath = "") : SpatialActionNodeBase2D(nodePath)
{
	/// <summary>
	/// Input property index for the scale.
	/// </summary>
	public const byte ScaleInput = 1;

	/// <inheritdoc/>
	public override string Description => "Sets an entity's scale.";

	/// <inheritdoc/>
	protected override void DefineSpatialParameters(
		List<InputProperty> inputProperties,
		List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Scale", typeof(NumericsVector2)));
	}

	/// <inheritdoc/>
	protected override void ExecuteOn(Node2D spatialNode, GraphContext graphContext)
	{
		if (graphContext.TryResolve(InputProperties[ScaleInput].BoundName, out NumericsVector2 scale))
		{
			spatialNode.Scale = new Vector2(scale.X, scale.Y);
		}
	}
}
