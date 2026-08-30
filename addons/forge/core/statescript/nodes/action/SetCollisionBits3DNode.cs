// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;

/// <summary>
/// Action node that turns collision layers or mask bits on or off, permanently.
/// </summary>
/// <remarks>
/// Nothing restores what this writes, which is the point: use it for a change that should outlive the node that made
/// it, such as a corpse dropping out of the targeting layer. For anything that lasts only as long as an ability, use
/// Collision Override 3D, which puts the original bits back even when the ability is interrupted.
/// </remarks>
/// <param name="target">Which of the two bit fields to write.</param>
/// <param name="operation">Whether the given bits are turned on or off.</param>
/// <param name="nodePath">Optional path to a descendant node to write instead of the entity's own spatial node.</param>
[StatescriptCategory("Physics")]
public sealed class SetCollisionBits3DNode(
	CollisionBitsTarget target = CollisionBitsTarget.Layer,
	CollisionBitsOperation operation = CollisionBitsOperation.Clear,
	string nodePath = "") : SpatialActionNodeBase3D(nodePath)
{
	/// <summary>
	/// Input property index for the bits to act on.
	/// </summary>
	public const byte BitsInput = 1;

	private readonly CollisionBitsTarget _target = target;
	private readonly CollisionBitsOperation _operation = operation;

	/// <inheritdoc/>
	public override string Description => "Turns collision layer or mask bits on or off, without restoring them.";

	/// <inheritdoc/>
	protected override void DefineSpatialParameters(
		List<InputProperty> inputProperties,
		List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Bits", typeof(int)));
	}

	/// <inheritdoc/>
	protected override void ExecuteOn(Node3D spatialNode, GraphContext graphContext)
	{
		if (spatialNode is not CollisionObject3D body)
		{
			ReportUnusableNodeOnce(
				$"resolved to a {spatialNode.GetType().Name}, which has no collision layers to set. The write was" +
				" skipped.");

			return;
		}

		if (!graphContext.TryResolve(InputProperties[BitsInput].BoundName, out int bits))
		{
			return;
		}

		CollisionBits3D.Write(
			body,
			_target,
			CollisionBits3D.Apply(CollisionBits3D.Read(body, _target), unchecked((uint)bits), _operation));
	}
}
