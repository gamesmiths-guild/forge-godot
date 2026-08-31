// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;

/// <summary>
/// Base for the action nodes that instantiate a scene into the running game and forget about it.
/// </summary>
/// <remarks>
/// <para>Everything about instantiating is dimension-neutral except the transform the graph hands over, so the pair
/// share this base and differ only in the two rows that place what they made. Declaring the transform in the middle of
/// the parameter list is what keeps the operand indexes identical across the pair, which is what lets one node editor
/// serve both.</para>
/// <para>The instance node and, when the scene is a Forge entity, that entity are written to output variables, so the
/// graph can keep acting on what it made.</para>
/// </remarks>
/// <param name="parentMode">Where the instance is parented.</param>
/// <param name="passOwnership">Whether to tell the instance who instantiated it, when its root implements
/// <see cref="IInstantiationReceiver"/>.</param>
public abstract class InstantiateSceneNodeBase(
	InstantiateParentMode parentMode = InstantiateParentMode.CurrentScene,
	bool passOwnership = true) : ActionNode
{
	/// <summary>
	/// Input property index for the scene to instantiate.
	/// </summary>
	public const byte SceneInput = 0;

	/// <summary>
	/// Input property index for the optional world position.
	/// </summary>
	public const byte PositionInput = 1;

	/// <summary>
	/// Input property index for the optional world rotation.
	/// </summary>
	public const byte RotationInput = 2;

	/// <summary>
	/// Input property index for the entity the instance is parented to under
	/// <see cref="InstantiateParentMode.Entity"/>, and placed at when no position is bound. Unbound means the
	/// ability's owner.
	/// </summary>
	public const byte ParentEntityInput = 3;

	/// <summary>
	/// Input property index for the parent node used under <see cref="InstantiateParentMode.Node"/>.
	/// </summary>
	public const byte ParentNodeInput = 4;

	/// <summary>
	/// Output variable index for the instance node.
	/// </summary>
	public const byte InstanceOutput = 0;

	/// <summary>
	/// Output variable index for the instance entity, when the scene is one.
	/// </summary>
	public const byte InstanceEntityOutput = 1;

	/// <summary>
	/// Adds the position and rotation rows, in this node's own dimension.
	/// </summary>
	/// <param name="inputProperties">The input property list to add to.</param>
	protected abstract void DefineTransformParameters(List<InputProperty> inputProperties);

	/// <summary>
	/// Instantiates the scene, resolving this node's own dimension of transform on the way.
	/// </summary>
	/// <param name="graphContext">The graph execution context.</param>
	/// <param name="scene">The scene to instantiate.</param>
	/// <param name="parentNode">The resolved parent node operand.</param>
	/// <param name="parentEntity">The resolved parent entity operand.</param>
	/// <returns>The instance, or <see langword="null"/> when it could not be parented.</returns>
	protected abstract Node? Instantiate(
		GraphContext graphContext,
		PackedScene scene,
		Node? parentNode,
		IForgeEntity? parentEntity);

	/// <summary>
	/// Gets where the instance is parented.
	/// </summary>
	protected InstantiateParentMode ParentMode { get; } = parentMode;

	/// <summary>
	/// Gets a value indicating whether the instance is told who instantiated it.
	/// </summary>
	protected bool PassOwnership { get; } = passOwnership;

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Scene", typeof(PackedScene)));
		DefineTransformParameters(inputProperties);
		inputProperties.Add(new InputProperty("Parent Entity", typeof(IForgeEntity), IsOptional: true));
		inputProperties.Add(new InputProperty("Parent Node", typeof(Node), IsOptional: true));

		outputVariables.Add(new OutputVariable("Instance", typeof(Node)));
		outputVariables.Add(new OutputVariable("Instance Entity", typeof(IForgeEntity)));
	}

	/// <inheritdoc/>
	protected override void Execute(GraphContext graphContext)
	{
		if (!graphContext.TryResolveObject(InputProperties[SceneInput].BoundName, out PackedScene? scene)
			|| scene is null)
		{
			return;
		}

		Node? instance = Instantiate(
			graphContext,
			scene,
			SceneInstantiationInputs.ResolveParentNode(graphContext, InputProperties[ParentNodeInput].BoundName),
			SceneInstantiationInputs.ResolveEntityOrOwner(graphContext, InputProperties[ParentEntityInput].BoundName));

		if (instance is null)
		{
			return;
		}

		SceneInstantiationInputs.WriteObjectOutput(graphContext, OutputVariables[InstanceOutput], instance);

		SceneInstantiationInputs.WriteObjectOutput(
			graphContext,
			OutputVariables[InstanceEntityOutput],
			ForgeEntityBridge.TryGetEntity(instance, out IForgeEntity? instanceEntity) ? instanceEntity : null);
	}
}
