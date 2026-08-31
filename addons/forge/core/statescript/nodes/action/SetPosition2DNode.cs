// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;

/// <summary>
/// Action node that moves an entity to a position, instantly.
/// </summary>
/// <remarks>
/// This is a teleport, not a movement: nothing is swept, so a destination inside geometry stays inside geometry. For
/// travel that can be interrupted or that takes time, use Move To 2D instead.
/// </remarks>
/// <param name="space">Whether the position is world or parent-relative.</param>
/// <param name="nodePath">Optional path to a descendant node to move instead of the entity's own spatial node.</param>
[StatescriptCategory("Spatial")]
public sealed class SetPosition2DNode(TransformSpace space = TransformSpace.Global, string nodePath = "")
	: SpatialActionNodeBase2D(nodePath)
{
	/// <summary>
	/// Input property index for the destination.
	/// </summary>
	public const byte PositionInput = 1;

	private readonly TransformSpace _space = space;

	/// <inheritdoc/>
	public override string Description => "Moves an entity to a position instantly.";

	/// <inheritdoc/>
	protected override void DefineSpatialParameters(
		List<InputProperty> inputProperties,
		List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Position", typeof(NumericsVector2)));
	}

	/// <inheritdoc/>
	protected override void ExecuteOn(Node2D spatialNode, GraphContext graphContext)
	{
		if (!graphContext.TryResolve(InputProperties[PositionInput].BoundName, out NumericsVector2 position))
		{
			return;
		}

		var target = new Vector2(position.X, position.Y);

		if (_space == TransformSpace.Local)
		{
			spatialNode.Position = target;
			return;
		}

		spatialNode.GlobalPosition = target;
	}
}
