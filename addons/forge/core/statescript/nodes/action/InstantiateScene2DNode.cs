// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Godot;
using Node = Godot.Node;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;

/// <summary>
/// Action node that instantiates a 2D scene into the running game and forgets about it.
/// </summary>
/// <remarks>
/// The instance outlives the node, and the graph, unless something else frees it. That is the point: a projectile
/// should keep flying after the ability that fired it has ended. Use Scene 2D instead for anything whose lifetime
/// should match a state. Aim is the rotation it is instantiated with, so binding Rotation to an angle is the whole
/// launch story for a Forge Projectile 2D.
/// </remarks>
/// <param name="parentMode">Where the instance is parented.</param>
/// <param name="passOwnership">Whether to tell the instance who instantiated it, when its root implements
/// <see cref="IInstantiationReceiver"/>.</param>
[StatescriptCategory("Scene")]
public sealed class InstantiateScene2DNode(
	InstantiateParentMode parentMode = InstantiateParentMode.CurrentScene,
	bool passOwnership = true) : InstantiateSceneNodeBase(parentMode, passOwnership)
{
	/// <inheritdoc/>
	public override string Description => "Instantiates a 2D scene into the running game, without owning its lifetime.";

	/// <inheritdoc/>
	protected override void DefineTransformParameters(List<InputProperty> inputProperties)
	{
		inputProperties.Add(new InputProperty("Position", typeof(NumericsVector2), IsOptional: true));
		inputProperties.Add(new InputProperty("Rotation", typeof(double), IsOptional: true));
	}

	/// <inheritdoc/>
	protected override Node? Instantiate(
		GraphContext graphContext,
		PackedScene scene,
		Node? parentNode,
		IForgeEntity? parentEntity)
	{
		return SceneInstantiationUtilities.Instantiate2D(
			graphContext,
			scene,
			ParentMode,
			parentNode,
			parentEntity,
			SceneInstantiationInputs.ResolveOptionalVector2(graphContext, InputProperties[PositionInput].BoundName),
			SceneInstantiationInputs.ResolveOptionalAngle(graphContext, InputProperties[RotationInput].BoundName),
			PassOwnership);
	}
}
