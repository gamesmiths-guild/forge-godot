// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Godot;
using NumericsQuaternion = System.Numerics.Quaternion;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;

/// <summary>
/// Action node that sets an entity's rotation, instantly.
/// </summary>
/// <param name="space">Whether the rotation is world or parent-relative.</param>
/// <param name="nodePath">Optional path to a descendant node to rotate instead of the entity's own spatial node.
/// </param>
[StatescriptCategory("Spatial")]
public sealed class SetRotation3DNode(TransformSpace space = TransformSpace.Global, string nodePath = "")
	: SpatialActionNodeBase3D(nodePath)
{
	/// <summary>
	/// Input property index for the rotation.
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
		inputProperties.Add(new InputProperty("Rotation", typeof(NumericsQuaternion)));
	}

	/// <inheritdoc/>
	protected override void ExecuteOn(Node3D spatialNode, GraphContext graphContext)
	{
		if (!graphContext.TryResolve(InputProperties[RotationInput].BoundName, out NumericsQuaternion rotation))
		{
			return;
		}

		Quaternion target = new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W).Normalized();

		if (_space == TransformSpace.Local)
		{
			spatialNode.Quaternion = target;
			return;
		}

		// Scale is carried separately so a scaled node keeps its scale, which assigning a pure rotation basis would
		// drop.
		Vector3 scale = spatialNode.GlobalBasis.Scale;
		spatialNode.GlobalBasis = new Basis(target).Scaled(scale);
	}
}
