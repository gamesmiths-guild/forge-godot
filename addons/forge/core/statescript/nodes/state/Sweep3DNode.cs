// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Ports;
using Godot;
using GodotRidArray = Godot.Collections.Array<Godot.Rid>;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// State node that sweeps a shape every tick while active, reporting when it starts and stops meeting something.
/// </summary>
/// <remarks>
/// <para>This is Shapecast 3D held open, and it stands to that node exactly as Ray 3D stands to Raycast 3D — the
/// shorter name is the monitored one in both pairs. The sweep inputs are re-resolved on every cast, so binding the
/// origin and direction to spatial getters makes the volume follow whoever is moving.</para>
/// <para>What it is for is the check a ray is too thin to make honestly: whether the path a body is about to travel is
/// still clear. A dash that cancels the moment its corridor closes, a charge that stops at the first thing wide enough
/// to stop it, a shield that reports when something enters the space in front of it.</para>
/// <para>The port shape mirrors Condition Monitor and Ray 3D: two edges for the transitions and two subgraphs for the
/// states between them, so what happens while the path is blocked and what happens while it is clear each live in
/// their own branch without a variable to tell them apart.</para>
/// <para>With <paramref name="oneShot"/> the node emits <see cref="OnHitPort"/> and deactivates on the first thing it
/// meets, which is the "travel until something is in the way" form.</para>
/// </remarks>
/// <param name="collideWithAreas">Whether areas stop the sweep, as well as bodies.</param>
/// <param name="oneShot">Whether the node deactivates itself the first time the sweep meets something.</param>
[StatescriptCategory("Physics")]
public class Sweep3DNode(bool collideWithAreas = false, bool oneShot = false) : StateNode<Sweep3DNodeContext>
{
	/// <summary>
	/// Output port index for the event emitted when the sweep starts meeting something.
	/// </summary>
	public const byte OnHitPort = 4;

	/// <summary>
	/// Output port index for the event emitted when the sweep stops meeting anything.
	/// </summary>
	public const byte OnLostPort = 5;

	/// <summary>
	/// Output port index for the subgraph that is active while the sweep is meeting something.
	/// </summary>
	public const byte WhileHitPort = 6;

	/// <summary>
	/// Output port index for the subgraph that is active while the sweep is meeting nothing.
	/// </summary>
	public const byte WhileClearPort = 7;

	private readonly bool _collideWithAreas = collideWithAreas;
	private readonly bool _oneShot = oneShot;
	private readonly GodotRidArray _exclusions = [];

	/// <inheritdoc/>
	public override string Description =>
		"Sweeps a shape every tick while active, emitting transition events and routing hit and clear subgraphs.";

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
		ShapecastNodeParameters3D.Define(inputProperties, outputVariables);
	}

	/// <inheritdoc/>
	protected override void OnActivate(GraphContext graphContext)
	{
		graphContext.GetNodeContext<Sweep3DNodeContext>(NodeID).LastHit = null;
		Cast(graphContext);
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
		Sweep3DNodeContext nodeContext = graphContext.GetNodeContext<Sweep3DNodeContext>(NodeID);

		PhysicsDebugDraw3D.Release(nodeContext.DebugMarker);
		nodeContext.DebugMarker = null;
	}

	/// <inheritdoc/>
	protected override void OnFixedUpdate(double deltaTime, GraphContext graphContext)
	{
		Cast(graphContext);
	}

	private void Cast(GraphContext graphContext)
	{
		bool hit = ShapecastNodeParameters3D.TryCast(
			graphContext,
			InputProperties,
			_collideWithAreas,
			_exclusions,
			out Shape3D? shape,
			out RaycastResult3D result,
			out Transform3D hitTransform,
			out RaySegment3D segment);

		// Written on every cast, not only on a transition: a sweep that stays blocked still needs the moving contact
		// point whatever reacts to it is placed at.
		ShapecastNodeParameters3D.WriteOutputs(graphContext, OutputVariables, result);

		Sweep3DNodeContext nodeContext = graphContext.GetNodeContext<Sweep3DNodeContext>(NodeID);

		// Held for the node's lifetime and recoloured on each cast, so a watched path shows both where it reaches and
		// whether it is currently blocked - and released when there is nothing to cast, since a marker left standing
		// would go on claiming the last sweep's outline and colour for a sweep that is no longer running.
		if (shape is null)
		{
			PhysicsDebugDraw3D.Release(nodeContext.DebugMarker);
			nodeContext.DebugMarker = null;
		}
		else
		{
			nodeContext.DebugMarker = PhysicsDebugDraw3D.EnsureMarker(
				graphContext,
				nodeContext.DebugMarker,
				hit ? PhysicsDebugDraw3D.RayHitColor : PhysicsDebugDraw3D.RayClearColor);

			PhysicsDebugDraw3D.SetShapecast(
				nodeContext.DebugMarker,
				shape,
				hitTransform,
				segment.From,
				segment.To);
		}

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
