// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Godot;
using Node = Godot.Node;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// State node that owns an instantiated 2D scene for as long as it is active, freeing it on deactivation.
/// </summary>
/// <param name="parentMode">Where the instance is parented.</param>
/// <param name="passOwnership">Whether to tell the instance who instantiated it.</param>
[StatescriptCategory("Scene")]
public class Scene2DNode(
	InstantiateParentMode parentMode = InstantiateParentMode.CurrentScene,
	bool passOwnership = true) : SceneNodeBase(parentMode, passOwnership)
{
	/// <inheritdoc/>
	public override string Description =>
		"Instantiates a 2D scene while active and frees it on deactivation, with an optional lifetime.";

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
