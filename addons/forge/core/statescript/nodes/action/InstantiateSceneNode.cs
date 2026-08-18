// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Godot;
using Node = Godot.Node;
using NumericsQuaternion = System.Numerics.Quaternion;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;

/// <summary>
/// Action node that instantiates a scene into the running game and forgets about it.
/// </summary>
/// <remarks>
/// <para>The instance outlives the node, and the graph, unless something else frees it. That is the point: a projectile
/// should keep flying after the ability that fired it has ended. Use Scene Instance instead for anything whose lifetime
/// should match a state.</para>
/// <para>The instance node and, when the scene is a Forge entity, that entity are written to output variables, so the
/// graph can keep acting on what it made.</para>
/// </remarks>
/// <param name="parentMode">Where the instance is parented.</param>
/// <param name="passOwnership">Whether to tell the instance who instance it, when its root implements
/// <see cref="IInstantiationReceiver"/>.</param>
[StatescriptCategory("Scene")]
public sealed class InstantiateSceneNode(
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

	private readonly InstantiateParentMode _parentMode = parentMode;
	private readonly bool _passOwnership = passOwnership;

	/// <inheritdoc/>
	public override string Description => "Instantiates a scene into the running game, without owning its lifetime.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Scene", typeof(PackedScene)));
		inputProperties.Add(new InputProperty("Position", typeof(NumericsVector3), IsOptional: true));
		inputProperties.Add(new InputProperty("Rotation", typeof(NumericsQuaternion), IsOptional: true));
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

		Node? instance = SceneInstantiationUtilities.Instantiate(
			graphContext,
			scene,
			_parentMode,
			SceneInstantiationInputs.ResolveParentNode(graphContext, InputProperties[ParentNodeInput].BoundName),
			SceneInstantiationInputs.ResolveEntityOrOwner(graphContext, InputProperties[ParentEntityInput].BoundName),
			SceneInstantiationInputs.ResolveOptionalVector3(graphContext, InputProperties[PositionInput].BoundName),
			SceneInstantiationInputs.ResolveOptionalQuaternion(graphContext, InputProperties[RotationInput].BoundName),
			_passOwnership);

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
