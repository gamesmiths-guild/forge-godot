// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Ports;
using GodotRidArray = Godot.Collections.Array<Godot.Rid>;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// State node that casts a ray every tick while active, reporting when it starts and stops hitting something.
/// </summary>
/// <remarks>
/// <para>This is Raycast 3D held open. The ray inputs are re-resolved on every cast, so binding the origin and
/// direction to spatial getters makes the ray follow whoever is aiming it: a beam that sweeps, an aim lock that tracks,
/// a tether that breaks when the line is broken.</para>
/// <para>The port shape mirrors Condition Monitor: two edges for the transitions and two subgraphs for the states
/// between them, so a beam's continuous damage lives in <see cref="WhileHitPort"/> and its search animation in
/// <see cref="WhileClearPort"/> without either needing a variable to know which is running.</para>
/// <para>With <paramref name="oneShot"/> the node emits <see cref="OnHitPort"/> and deactivates on the first hit, which
/// is the "wait until the line is clear to fire" form.</para>
/// </remarks>
/// <param name="collideWithAreas">Whether areas count as hits, as well as bodies.</param>
/// <param name="hitFromInside">Whether a ray starting inside a shape reports that shape.</param>
/// <param name="oneShot">Whether the node deactivates itself the first time the ray hits.</param>
[StatescriptCategory("Physics")]
public class Ray3DNode(
	bool collideWithAreas = false,
	bool hitFromInside = false,
	bool oneShot = false) : StateNode<Ray3DNodeContext>
{
	/// <summary>
	/// Output port index for the event emitted when the ray starts hitting something.
	/// </summary>
	public const byte OnHitPort = 4;

	/// <summary>
	/// Output port index for the event emitted when the ray stops hitting anything.
	/// </summary>
	public const byte OnLostPort = 5;

	/// <summary>
	/// Output port index for the subgraph that is active while the ray is hitting something.
	/// </summary>
	public const byte WhileHitPort = 6;

	/// <summary>
	/// Output port index for the subgraph that is active while the ray is hitting nothing.
	/// </summary>
	public const byte WhileClearPort = 7;

	private readonly bool _collideWithAreas = collideWithAreas;
	private readonly bool _hitFromInside = hitFromInside;
	private readonly bool _oneShot = oneShot;
	private readonly GodotRidArray _exclusions = [];

	/// <inheritdoc/>
	public override string Description =>
		"Casts a ray every tick while active, emitting transition events and routing hit and clear subgraphs.";

	/// <inheritdoc/>
	protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
	{
		base.DefinePorts(inputPorts, outputPorts);
		outputPorts.Add(CreatePort<EventPort>(OnHitPort, "OnHit"));
		outputPorts.Add(CreatePort<EventPort>(OnLostPort, "OnLost"));
		outputPorts.Add(CreatePort<SubgraphPort>(WhileHitPort, "WhileHit"));
		outputPorts.Add(CreatePort<SubgraphPort>(WhileClearPort, "WhileClear"));
	}

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		RaycastNodeParameters3D.Define(inputProperties, outputVariables);
	}

	/// <inheritdoc/>
	protected override void OnActivate(GraphContext graphContext)
	{
		graphContext.GetNodeContext<Ray3DNodeContext>(NodeID).LastHit = null;
		Cast(graphContext);
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
		Ray3DNodeContext nodeContext =
			graphContext.GetNodeContext<Ray3DNodeContext>(NodeID);

		PhysicsDebugDraw3D.Release(nodeContext.DebugMarker);
		nodeContext.DebugMarker = null;
	}

	/// <inheritdoc/>
	protected override void OnUpdate(double deltaTime, GraphContext graphContext)
	{
		Cast(graphContext);
	}

	private void Cast(GraphContext graphContext)
	{
		bool hit = RaycastNodeParameters3D.TryCast(
			graphContext,
			InputProperties,
			_collideWithAreas,
			_hitFromInside,
			_exclusions,
			out RaycastResult3D result,
			out RaySegment3D segment);

		// Written on every cast, not only on a transition: a beam that stays on a target still needs the moving hit
		// point its damage and its visual effect are placed at.
		RaycastNodeParameters3D.WriteOutputs(graphContext, OutputVariables, result);

		Ray3DNodeContext nodeContext =
			graphContext.GetNodeContext<Ray3DNodeContext>(NodeID);

		// Held for the node's lifetime and recoloured on each cast, so a beam shows both where it reaches and whether
		// it is currently on something.
		nodeContext.DebugMarker = PhysicsDebugDraw3D.EnsureMarker(
			graphContext,
			nodeContext.DebugMarker,
			hit ? PhysicsDebugDraw3D.RayHitColor : PhysicsDebugDraw3D.RayClearColor);

		PhysicsDebugDraw3D.SetLine(nodeContext.DebugMarker, segment.From, segment.To);

		if (nodeContext.LastHit == hit)
		{
			return;
		}

		bool hadValue = nodeContext.LastHit.HasValue;
		nodeContext.LastHit = hit;

		if (hit && _oneShot)
		{
			DeactivateNodeAndEmitMessage(graphContext, OnHitPort);
			return;
		}

		if (hadValue)
		{
			var previousSubgraphPort = (SubgraphPort)OutputPorts[hit ? WhileClearPort : WhileHitPort];
			previousSubgraphPort.EmitDisableSubgraphMessage(graphContext);
		}

		if (hit)
		{
			EmitMessage(graphContext, OnHitPort, WhileHitPort);
			return;
		}

		EmitMessage(graphContext, OnLostPort, WhileClearPort);
	}
}
