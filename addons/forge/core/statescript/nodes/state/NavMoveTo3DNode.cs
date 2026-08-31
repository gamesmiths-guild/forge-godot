// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Ports;
using Godot;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// State node that steers an entity to a destination along a navigation path, emitting <see cref="OnReachedPort"/> on
/// arrival and <see cref="OnFailedPort"/> when the destination cannot be reached.
/// </summary>
/// <remarks>
/// <para>This is the pathfinding counterpart of Move To 3D, and the two are opposites on purpose. Move To drives the
/// transform along a straight line and passes through geometry; this drives the body's <em>velocity</em> from a
/// <see cref="NavigationAgent3D"/> already in the scene, so it goes round walls and the game's own movement code still
/// does the moving. A summon walking to a marked spot, a charge that follows the corridor rather than the wall it is
/// pointed at.</para>
/// <para>The agent is authored, never created. Avoidance radius, path desired distance, the navigation layers it may
/// walk on - all of it is scene data belonging to the character, not to one ability, and an agent conjured with
/// guessed values would path differently from every other agent in the game. An empty path takes the entity's own
/// agent child, matching the presentation nodes.</para>
/// <para>The destination is re-read every update, so binding it to a spatial getter makes this a chase rather than a
/// walk to where something was.</para>
/// <para>Under <paramref name="useSafeVelocity"/> the agent's avoidance solver decides the velocity and answers on its
/// own schedule, which costs one frame of standing still at the start and means the body follows the solver rather
/// than the path directly. Leave it off unless the agent has avoidance enabled and crowding is the problem being
/// solved.</para>
/// <para>Reachability is judged only once a physics frame has passed. A navigation map that has not synced yet hands
/// back an empty path, and an empty path reports every destination as unreachable - so judging on the first update
/// would fail every walk that started on the frame its level loaded.</para>
/// <para>Deactivating zeroes the body's velocity, which reaches arrival, failure and abort alike: a summon whose order
/// is cancelled stops rather than coasting on the last velocity the path asked for.</para>
/// <para>Configuration is captured in field initializers rather than a constructor body, because the base node
/// constructor calls <see cref="DefinePorts"/> and <see cref="DefineParameters"/> before a body would run.</para>
/// </remarks>
/// <param name="agentPath">Optional path to the navigation agent, from the node the entity lives on. Empty means the
/// entity's first agent child.</param>
/// <param name="useSafeVelocity">Whether the body follows the agent's avoidance-adjusted velocity rather than the path
/// directly.</param>
[StatescriptCategory("Navigation")]
public class NavMoveTo3DNode(string agentPath = "", bool useSafeVelocity = false)
	: StateNode<NavMoveTo3DNodeContext>
{
	/// <summary>
	/// Input property index for the entity to steer. Unbound means the ability's owner.
	/// </summary>
	public const byte EntityInput = 0;

	/// <summary>
	/// Input property index for the destination.
	/// </summary>
	public const byte TargetInput = 1;

	/// <summary>
	/// Input property index for the travel speed, in units per second.
	/// </summary>
	public const byte SpeedInput = 2;

	/// <summary>
	/// Output port index for the event emitted when the entity reaches the destination.
	/// </summary>
	public const byte OnReachedPort = 4;

	/// <summary>
	/// Output port index for the event emitted when the destination cannot be reached.
	/// </summary>
	public const byte OnFailedPort = 5;

	private readonly string _agentPath = agentPath ?? string.Empty;
	private readonly bool _useSafeVelocity = useSafeVelocity;

	private bool _reportedMissingAgent;
	private bool _reportedUnusableBody;

	/// <inheritdoc/>
	public override string Description =>
		"Steers an entity to a destination along a navigation path, emitting OnReached or OnFailed.";

	/// <inheritdoc/>
	protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
	{
		base.DefinePorts(inputPorts, outputPorts);
		outputPorts.Add(CreatePort<EventPort>(OnReachedPort, "OnReached"));
		outputPorts.Add(CreatePort<EventPort>(OnFailedPort, "OnFailed"));
	}

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Entity", typeof(IForgeEntity), IsOptional: true));
		inputProperties.Add(new InputProperty("Target", typeof(NumericsVector3)));
		inputProperties.Add(new InputProperty("Speed", typeof(double)));
	}

	/// <inheritdoc/>
	protected override void OnActivate(GraphContext graphContext)
	{
		NavMoveTo3DNodeContext nodeContext = graphContext.GetNodeContext<NavMoveTo3DNodeContext>(NodeID);
		nodeContext.Agent = null;
		nodeContext.SafeVelocity = Vector3.Zero;
		nodeContext.ActivationPhysicsFrame = Engine.GetPhysicsFrames();

		IForgeEntity? entity = ResolveEntityOrOwner(graphContext);

		if (!ForgeEntityBridge.TryGetEntityChild(entity, _agentPath, out NavigationAgent3D? agent))
		{
			ReportMissingAgentOnce();
			return;
		}

		nodeContext.Agent = agent;

		if (!_useSafeVelocity)
		{
			return;
		}

		var callable = Callable.From((Vector3 safeVelocity) => nodeContext.SafeVelocity = safeVelocity);
		agent.Connect(NavigationAgent3D.SignalName.VelocityComputed, callable);
		nodeContext.VelocityComputed = callable;
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
		NavMoveTo3DNodeContext nodeContext = graphContext.GetNodeContext<NavMoveTo3DNodeContext>(NodeID);
		NavigationAgent3D? agent = nodeContext.Agent;

		if (nodeContext.VelocityComputed is Callable callable
			&& agent is not null
			&& GodotObject.IsInstanceValid(agent))
		{
			agent.Disconnect(NavigationAgent3D.SignalName.VelocityComputed, callable);
		}

		nodeContext.VelocityComputed = null;
		nodeContext.Agent = null;

		// Stopping on the way out covers arrival, failure and abort with one rule. A body left holding the last
		// velocity the path asked for would keep walking into whatever the ability was interrupted by.
		if (ForgeEntityBridge.TryGetSpatialNode3D(ResolveEntityOrOwner(graphContext), out Node3D? spatialNode))
		{
			TryApplyVelocity(spatialNode, Vector3.Zero);
		}
	}

	/// <inheritdoc/>
	protected override void OnUpdate(double deltaTime, GraphContext graphContext)
	{
		NavMoveTo3DNodeContext nodeContext = graphContext.GetNodeContext<NavMoveTo3DNodeContext>(NodeID);
		NavigationAgent3D? agent = nodeContext.Agent;

		// A walk with no agent is over rather than pending: nothing supplies one after activation, so holding the node
		// open would stall every graph waiting on either port. Activation already warned about why.
		if (agent is null || !GodotObject.IsInstanceValid(agent))
		{
			DeactivateNodeAndEmitMessage(graphContext, OnFailedPort);
			return;
		}

		if (!ForgeEntityBridge.TryGetSpatialNode3D(ResolveEntityOrOwner(graphContext), out Node3D? spatialNode))
		{
			return;
		}

		if (!graphContext.TryResolve(InputProperties[TargetInput].BoundName, out NumericsVector3 target))
		{
			DeactivateNodeAndEmitMessage(graphContext, OnFailedPort);
			return;
		}

		graphContext.TryResolve(InputProperties[SpeedInput].BoundName, out double speed);

		// Submitted before anything is asked of the agent: its path is recomputed lazily, and every query below is a
		// no-op until a destination has been handed to it.
		agent.TargetPosition = new Vector3(target.X, target.Y, target.Z);

		if (agent.IsNavigationFinished())
		{
			DeactivateNodeAndEmitMessage(graphContext, OnReachedPort);
			return;
		}

		Vector3 next = agent.GetNextPathPosition();

		if (Engine.GetPhysicsFrames() > nodeContext.ActivationPhysicsFrame && !agent.IsTargetReachable())
		{
			DeactivateNodeAndEmitMessage(graphContext, OnFailedPort);
			return;
		}

		Vector3 desired = (next - spatialNode.GlobalPosition).Normalized() * (float)speed;

		if (!_useSafeVelocity)
		{
			TryApplyVelocity(spatialNode, desired);
			return;
		}

		// The solver answers on its own schedule, so what is applied is the answer to the previous frame's question.
		// Submitting first keeps that lag to one frame rather than two.
		agent.Velocity = desired;
		TryApplyVelocity(spatialNode, nodeContext.SafeVelocity);
	}

	private void TryApplyVelocity(Node3D spatialNode, Vector3 velocity)
	{
		switch (spatialNode)
		{
			case CharacterBody3D characterBody:
				characterBody.Velocity = velocity;
				break;

			case RigidBody3D rigidBody:
				rigidBody.LinearVelocity = velocity;
				break;

			default:
				ReportUnusableBodyOnce(spatialNode);
				break;
		}
	}

	private IForgeEntity? ResolveEntityOrOwner(GraphContext graphContext)
	{
		StringKey boundName = InputProperties[EntityInput].BoundName;

		if (boundName != StringKey.Empty
			&& graphContext.TryResolveObject(boundName, out IForgeEntity? entity)
			&& entity is not null)
		{
			return entity;
		}

		return graphContext.TryGetActivationContext(out AbilityBehaviorContext? abilityContext)
			? abilityContext.Owner
			: null;
	}

	private void ReportMissingAgentOnce()
	{
		if (_reportedMissingAgent)
		{
			return;
		}

		_reportedMissingAgent = true;

		GD.PushWarning(
			"Statescript: Nav Move To 3D found no NavigationAgent3D for its entity" +
			(_agentPath.Length == 0 ? "." : $" at [{_agentPath}].") +
			" The walk reported OnFailed.");
	}

	// Suppressed separately from the missing-agent warning: an entity can have an agent and still not be a body the
	// path can steer, and one warning silencing the other would leave that second failure invisible.
	private void ReportUnusableBodyOnce(Node3D spatialNode)
	{
		if (_reportedUnusableBody)
		{
			return;
		}

		_reportedUnusableBody = true;

		GD.PushWarning(
			$"Statescript: Nav Move To 3D resolved to a {spatialNode.GetType().Name}, which has no velocity to" +
			" steer. The entity was not moved.");
	}
}
