// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Ports;
using Godot;
using Node = Godot.Node;
using NumericsQuaternion = System.Numerics.Quaternion;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// State node that owns a instance scene for as long as it is active, freeing it on deactivation.
/// </summary>
/// <remarks>
/// <para>This is the difference from Instantiate Scene: what this creates is tied to the node's lifetime. A summon,
/// a zone, a held visual effect - anything that should disappear when the ability ends, is cancelled, or is interrupted
/// - belongs here, and gets cleaned up without the graph having to remember to free it on every exit path.</para>
/// <para>An optional lifetime deactivates the node early through <see cref="OnLifetimeEndPort"/>, which is how a timed
/// summon expires on its own while still being freed if the ability ends first.</para>
/// </remarks>
/// <param name="parentMode">Where the instance is parented.</param>
/// <param name="passOwnership">Whether to tell the instance who instance it.</param>
[StatescriptCategory("Scene")]
public class SceneNode(
	InstantiateParentMode parentMode = InstantiateParentMode.CurrentScene,
	bool passOwnership = true) : StateNode<SceneNodeContext>
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
	/// Input property index for the optional lifetime in seconds.
	/// </summary>
	public const byte LifetimeInput = 5;

	/// <summary>
	/// Output variable index for the instance node.
	/// </summary>
	public const byte InstanceOutput = 0;

	/// <summary>
	/// Output variable index for the instance entity, when the scene is one.
	/// </summary>
	public const byte InstanceEntityOutput = 1;

	/// <summary>
	/// Output port index for the event emitted when the lifetime runs out.
	/// </summary>
	public const byte OnLifetimeEndPort = 4;

	private readonly InstantiateParentMode _parentMode = parentMode;
	private readonly bool _passOwnership = passOwnership;

	/// <inheritdoc/>
	public override string Description =>
		"Instantiates a scene while active and frees it on deactivation, with an optional lifetime.";

	/// <inheritdoc/>
	protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
	{
		base.DefinePorts(inputPorts, outputPorts);
		outputPorts.Add(CreatePort<EventPort>(OnLifetimeEndPort, "OnLifetimeEnd"));
	}

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Scene", typeof(PackedScene)));
		inputProperties.Add(new InputProperty("Position", typeof(NumericsVector3), IsOptional: true));
		inputProperties.Add(new InputProperty("Rotation", typeof(NumericsQuaternion), IsOptional: true));
		inputProperties.Add(new InputProperty("Parent Entity", typeof(IForgeEntity), IsOptional: true));
		inputProperties.Add(new InputProperty("Parent Node", typeof(Node), IsOptional: true));
		inputProperties.Add(new InputProperty("Lifetime", typeof(double), IsOptional: true));

		outputVariables.Add(new OutputVariable("Instance", typeof(Node)));
		outputVariables.Add(new OutputVariable("Instance Entity", typeof(IForgeEntity)));
	}

	/// <inheritdoc/>
	protected override void OnActivate(GraphContext graphContext)
	{
		SceneNodeContext nodeContext = graphContext.GetNodeContext<SceneNodeContext>(NodeID);
		nodeContext.Instance = null;
		nodeContext.ElapsedTime = 0;
		nodeContext.Lifetime = 0;

		if (!graphContext.TryResolveObject(InputProperties[SceneInput].BoundName, out PackedScene? scene)
			|| scene is null)
		{
			return;
		}

		graphContext.TryResolve(InputProperties[LifetimeInput].BoundName, out double lifetime);
		nodeContext.Lifetime = lifetime;

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

		nodeContext.Instance = instance;

		SceneInstantiationInputs.WriteObjectOutput(graphContext, OutputVariables[InstanceOutput], instance);

		SceneInstantiationInputs.WriteObjectOutput(
			graphContext,
			OutputVariables[InstanceEntityOutput],
			ForgeEntityBridge.TryGetEntity(instance, out IForgeEntity? instanceEntity) ? instanceEntity : null);
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
		SceneNodeContext nodeContext = graphContext.GetNodeContext<SceneNodeContext>(NodeID);
		Node? instance = nodeContext.Instance;
		nodeContext.Instance = null;

		// Every deactivation path frees the instance, including an abort, which is the whole reason this node exists
		// rather than an instantiate followed by a free the graph has to remember on each exit.
		if (instance is not null && GodotObject.IsInstanceValid(instance))
		{
			instance.QueueFree();
		}
	}

	/// <inheritdoc/>
	protected override void OnUpdate(double deltaTime, GraphContext graphContext)
	{
		SceneNodeContext nodeContext = graphContext.GetNodeContext<SceneNodeContext>(NodeID);

		if (nodeContext.Lifetime <= 0)
		{
			return;
		}

		nodeContext.ElapsedTime += deltaTime;

		if (nodeContext.ElapsedTime >= nodeContext.Lifetime)
		{
			// OnDeactivate does the freeing, so the timed path and the interrupted path clean up identically.
			DeactivateNodeAndEmitMessage(graphContext, OnLifetimeEndPort);
		}
	}
}
