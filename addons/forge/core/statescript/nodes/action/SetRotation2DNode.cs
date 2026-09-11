// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;

/// <summary>
/// Action node that sets an entity's rotation, instantly.
/// </summary>
/// <remarks>
/// The rotation is an angle, not a quaternion: a plane has one axis to turn around, so the whole rotation is one
/// number. The value read at runtime is in radians; a constant is typed in degrees on the row and stored converted.
/// </remarks>
/// <param name="space">Whether the rotation is world or parent-relative.</param>
/// <param name="nodePath">Optional path to a descendant node to rotate instead of the entity's own spatial node.
/// </param>
[StatescriptCategory("Spatial")]
[StatescriptAngleInputs(RotationInput)]
public sealed class SetRotation2DNode(TransformSpace space = TransformSpace.Global, string nodePath = "")
	: SpatialActionNodeBase2D(nodePath)
{
	/// <summary>
	/// Input property index for the rotation. Read in radians, typed in degrees.
	/// </summary>
	public const byte RotationInput = 1;

	private readonly TransformSpace _space = space;

	/// <inheritdoc/>
	public override string Description => "Sets an entity's rotation instantly.";

	/// <inheritdoc/>
	protected override void DefineSpatialParameters(
		List<InputProperty> inputProperties,
		List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Rotation (deg)", typeof(double)));
	}

	/// <inheritdoc/>
	protected override void ExecuteOn(Node2D spatialNode, GraphContext graphContext)
	{
		if (!graphContext.TryResolve(InputProperties[RotationInput].BoundName, out double rotation))
		{
			return;
		}

		if (_space == TransformSpace.Local)
		{
			spatialNode.Rotation = (float)rotation;
		}
		else
		{
			spatialNode.GlobalRotation = (float)rotation;
		}

		spatialNode.ResetPhysicsInterpolation();
	}
}
