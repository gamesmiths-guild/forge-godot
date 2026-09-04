// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Ports;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// State node that steers an entity to a destination along a navigation path, emitting <see cref="OnReachedPort"/> on
/// arrival and <see cref="OnFailedPort"/> when the destination cannot be reached.
/// </summary>
/// <remarks>
/// <para>This is the pathfinding counterpart of Move To 2D, and the two are opposites on purpose. Move To drives the
/// transform along a straight line and passes through geometry; this drives the body's <em>velocity</em> from a
/// <see cref="NavigationAgent2D"/> already in the scene, so it goes round walls and the game's own movement code still
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
public class NavMoveTo2DNode(string agentPath = "", bool useSafeVelocity = false)
	: StateNode<NavMoveTo2DNodeContext>
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
	private bool _reportedUnusableSpeed;
	private bool _reportedAvoidanceDisabled;

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
		inputProperties.Add(new InputProperty("Target", typeof(NumericsVector2)));
		inputProperties.Add(new InputProperty("Speed", typeof(double)));
	}

	/// <inheritdoc/>
	protected override void OnActivate(GraphContext graphContext)
	{
		NavMoveTo2DNodeContext nodeContext = graphContext.GetNodeContext<NavMoveTo2DNodeContext>(NodeID);
		nodeContext.Agent = null;
		nodeContext.SafeVelocity = Vector2.Zero;
		nodeContext.ActivationPhysicsFrame = Engine.GetPhysicsFrames();
		nodeContext.HasSubmittedTarget = false;

		IForgeEntity? entity = ResolveEntityOrOwner(graphContext);

		if (!ForgeEntityBridge.TryGetEntityChild(entity, _agentPath, out NavigationAgent2D? agent))
		{
			ReportMissingAgentOnce();
			return;
		}

		nodeContext.Agent = agent;

		if (!_useSafeVelocity)
		{
			return;
		}

		var callable = Callable.From((Vector2 safeVelocity) => nodeContext.SafeVelocity = safeVelocity);
		agent.Connect(NavigationAgent2D.SignalName.VelocityComputed, callable);
		nodeContext.VelocityComputed = callable;
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
		NavMoveTo2DNodeContext nodeContext = graphContext.GetNodeContext<NavMoveTo2DNodeContext>(NodeID);
		NavigationAgent2D? agent = nodeContext.Agent;

		if (nodeContext.VelocityComputed is Callable callable
			&& agent is not null
			&& GodotObject.IsInstanceValid(agent))
		{
			agent.Disconnect(NavigationAgent2D.SignalName.VelocityComputed, callable);
		}

		nodeContext.VelocityComputed = null;
		nodeContext.Agent = null;

		// Stopping on the way out covers arrival, failure and abort with one rule. A body left holding the last
		// velocity the path asked for would keep walking into whatever the ability was interrupted by.
		if (ForgeEntityBridge.TryGetSpatialNode2D(ResolveEntityOrOwner(graphContext), out Node2D? spatialNode))
		{
			TryApplyVelocity(spatialNode, Vector2.Zero);
		}
	}

	/// <inheritdoc/>
	protected override void OnUpdate(double deltaTime, GraphContext graphContext)
	{
		NavMoveTo2DNodeContext nodeContext = graphContext.GetNodeContext<NavMoveTo2DNodeContext>(NodeID);
		NavigationAgent2D? agent = nodeContext.Agent;

		// A walk with no agent is over rather than pending: nothing supplies one after activation, so holding the node
		// open would stall every graph waiting on either port. Activation already warned about why.
		if (agent is null || !GodotObject.IsInstanceValid(agent))
		{
			DeactivateNodeAndEmitMessage(graphContext, OnFailedPort);
			return;
		}

		// A walk whose subject has gone is over rather than pending, the same as one that never had an agent: a death
		// or a despawn mid-walk must not leave the graph waiting on a port that can no longer fire.
		if (!ForgeEntityBridge.TryGetSpatialNode2D(ResolveEntityOrOwner(graphContext), out Node2D? spatialNode))
		{
			DeactivateNodeAndEmitMessage(graphContext, OnFailedPort);
			return;
		}

		if (!graphContext.TryResolve(InputProperties[TargetInput].BoundName, out NumericsVector2 target))
		{
			DeactivateNodeAndEmitMessage(graphContext, OnFailedPort);
			return;
		}

		graphContext.TryResolve(InputProperties[SpeedInput].BoundName, out double speed);

		if (speed <= 0)
		{
			ReportUnusableSpeedOnce(speed);
		}

		var targetPosition = new Vector2(target.X, target.Y);

		if (!nodeContext.HasSubmittedTarget || !nodeContext.SubmittedTarget.IsEqualApprox(targetPosition))
		{
			agent.TargetPosition = targetPosition;
			nodeContext.SubmittedTarget = targetPosition;
			nodeContext.HasSubmittedTarget = true;
		}

		if (agent.IsNavigationFinished())
		{
			DeactivateNodeAndEmitMessage(graphContext, OnReachedPort);
			return;
		}

		Vector2 next = agent.GetNextPathPosition();

		if (Engine.GetPhysicsFrames() > nodeContext.ActivationPhysicsFrame && !agent.IsTargetReachable())
		{
			DeactivateNodeAndEmitMessage(graphContext, OnFailedPort);
			return;
		}

		Vector2 desired = (next - spatialNode.GlobalPosition).Normalized() * Mathf.Max((float)speed, 0.0f);

		if (nodeContext.SafeVelocityActive)
		{
			agent.Velocity = desired;
			desired = nodeContext.SafeVelocity;
		}

		if (!TryApplyVelocity(spatialNode, desired))
		{
			DeactivateNodeAndEmitMessage(graphContext, OnFailedPort);
		}
	}

	private bool TryApplyVelocity(Node2D spatialNode, Vector2 velocity)
	{
		switch (spatialNode)
		{
			case CharacterBody2D characterBody:
				characterBody.Velocity = velocity;
				return true;

			case RigidBody2D rigidBody:
				rigidBody.LinearVelocity = velocity;
				return true;

			default:
				ReportUnusableBodyOnce(spatialNode);
				return false;
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
			"Statescript: Nav Move To 2D found no NavigationAgent2D for its entity" +
			(_agentPath.Length == 0 ? "." : $" at [{_agentPath}].") +
			" The walk reported OnFailed.");
	}

	private void ReportAvoidanceDisabledOnce()
	{
		if (_reportedAvoidanceDisabled)
		{
			return;
		}

		_reportedAvoidanceDisabled = true;

		GD.PushWarning(
			"Statescript: Nav Move To 2D was told to use safe velocity, but its NavigationAgent2D has avoidance" +
			" disabled and so never computes one. The walk follows the path directly instead. Turn on the agent's" +
			" Avoidance Enabled, or clear Use Safe Velocity on the node.");
	}

	private void ReportUnusableSpeedOnce(double speed)
	{
		if (_reportedUnusableSpeed)
		{
			return;
		}

		_reportedUnusableSpeed = true;

		GD.PushWarning(
			$"Statescript: Nav Move To 2D resolved a speed of {speed}, so the entity is not moving. Speed is" +
			" required - an unbound row resolves to zero. A negative speed cannot drive a path and is read as zero.");
	}

	private void ReportUnusableBodyOnce(Node2D spatialNode)
	{
		if (_reportedUnusableBody)
		{
			return;
		}

		_reportedUnusableBody = true;

		GD.PushWarning(
			$"Statescript: Nav Move To 2D resolved to a {spatialNode.GetType().Name}, which has no velocity to" +
			" steer. The entity was not moved.");
	}
}
