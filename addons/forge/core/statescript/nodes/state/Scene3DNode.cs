// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Godot;
using Node = Godot.Node;
using NumericsQuaternion = System.Numerics.Quaternion;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// State node that owns an instantiated 3D scene for as long as it is active, freeing it on deactivation.
/// </summary>
/// <param name="parentMode">Where the instance is parented.</param>
/// <param name="passOwnership">Whether to tell the instance who instantiated it.</param>
[StatescriptCategory("Scene")]
public class Scene3DNode(
	InstantiateParentMode parentMode = InstantiateParentMode.CurrentScene,
	bool passOwnership = true) : SceneNodeBase(parentMode, passOwnership)
{
	/// <inheritdoc/>
	public override string Description =>
		"Instantiates a 3D scene while active and frees it on deactivation, with an optional lifetime.";

	/// <inheritdoc/>
	protected override void DefineTransformParameters(List<InputProperty> inputProperties)
	{
		inputProperties.Add(new InputProperty("Position", typeof(NumericsVector3), IsOptional: true));
		inputProperties.Add(new InputProperty("Rotation", typeof(NumericsQuaternion), IsOptional: true));
	}

	/// <inheritdoc/>
	protected override Node? Instantiate(
		GraphContext graphContext,
		PackedScene scene,
		Node? parentNode,
		IForgeEntity? parentEntity)
	{
		return SceneInstantiationUtilities.Instantiate3D(
			graphContext,
			scene,
			ParentMode,
			parentNode,
			parentEntity,
			SceneInstantiationInputs.ResolveOptionalVector3(graphContext, InputProperties[PositionInput].BoundName),
			SceneInstantiationInputs.ResolveOptionalQuaternion(graphContext, InputProperties[RotationInput].BoundName),
			PassOwnership);
	}
}
