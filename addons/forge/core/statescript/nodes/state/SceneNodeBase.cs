// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Ports;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// Base for the state nodes that own an instantiated scene for as long as they are active, freeing it on deactivation.
/// </summary>
/// <remarks>
/// <para>This is the difference from Instantiate Scene: what these create is tied to the node's lifetime. A summon,
/// a zone, a held visual effect - anything that should disappear when the ability ends, is cancelled, or is interrupted
/// - belongs here, and gets cleaned up without the graph having to remember to free it on every exit path.</para>
/// <para>An optional lifetime deactivates the node early through <see cref="OnLifetimeEndPort"/>, which is how a timed
/// summon expires on its own while still being freed if the ability ends first.</para>
/// <para>A retrigger while the instance is alive is ignored, or with <paramref name="restartOnRetrigger"/> the instance
/// is freed and the scene instantiated again, with a fresh lifetime. The subgraph is disabled first and comes back
/// for the new instance, since whatever ran in it was working on the one that was freed.</para>
/// <para>As with the action pair, only the transform differs between the two dimensions, and declaring it in the middle
/// of the parameter list keeps the operand indexes identical across the pair.</para>
/// </remarks>
/// <param name="parentMode">Where the instance is parented.</param>
/// <param name="passOwnership">Whether to tell the instance who instantiated it.</param>
/// <param name="restartOnRetrigger">Whether a retrigger replaces the instance instead of being ignored.</param>
public abstract class SceneNodeBase(
	InstantiateParentMode parentMode = InstantiateParentMode.CurrentScene,
	bool passOwnership = true,
	bool restartOnRetrigger = false) : StateNode<SceneNodeContext>(restartOnRetrigger)
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
	protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
	{
		base.DefinePorts(inputPorts, outputPorts);
		outputPorts.Add(CreatePort<EventPort>(OnLifetimeEndPort, "OnLifetimeEnd"));
	}

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Scene", typeof(PackedScene)));
		DefineTransformParameters(inputProperties);
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

		// Cleared first, so an activation that spawns nothing - a restart whose scene can no longer be resolved -
		// leaves no output naming an instance from before, freed or about to be.
		SceneInstantiationInputs.WriteObjectOutput(graphContext, OutputVariables[InstanceOutput], null);
		SceneInstantiationInputs.WriteObjectOutput(graphContext, OutputVariables[InstanceEntityOutput], null);

		if (!graphContext.TryResolveObject(InputProperties[SceneInput].BoundName, out PackedScene? scene)
			|| scene is null)
		{
			return;
		}

		graphContext.TryResolve(InputProperties[LifetimeInput].BoundName, out double lifetime);
		nodeContext.Lifetime = lifetime;

		Node? instance = Instantiate(
			graphContext,
			scene,
			SceneInstantiationInputs.ResolveParentNode(graphContext, InputProperties[ParentNodeInput].BoundName),
			SceneInstantiationInputs.ResolveEntityOrOwner(graphContext, InputProperties[ParentEntityInput].BoundName));

		if (instance is null)
		{
			return;
		}

		// The instance readies and is handed its owner as it is added, and its own code can end this node or the
		// graph - an effect it applies can cancel the ability the graph runs for. The deactivation that would free it
		// has then already run, before the node held it.
		if (!nodeContext.Active)
		{
			instance.QueueFree();
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
	protected override void OnRestart(GraphContext graphContext)
	{
		SceneNodeContext nodeContext = graphContext.GetNodeContext<SceneNodeContext>(NodeID);
		((SubgraphPort)OutputPorts[SubgraphPort]).EmitDisableSubgraphMessage(graphContext);

		// Ending the subgraph can end this node too - an abort or an Exit reached from a child's OnDeactivate - and
		// then its instance is already freed, with nothing left to restart.
		if (!nodeContext.Active)
		{
			return;
		}

		OnDeactivate(graphContext);
		OnActivate(graphContext);
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
