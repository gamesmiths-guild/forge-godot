// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Ports;
using Godot;
using NumericsQuaternion = System.Numerics.Quaternion;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// State node that watches a volume while active, reporting the entities that enter and leave it.
/// </summary>
/// <remarks>
/// <para>The port shape mirrors Condition Monitor: two edges for the transitions and two subgraphs for the states
/// between them. The edges are per entity - <see cref="OnEnteredPort"/> and <see cref="OnExitedPort"/> fire once each
/// per entity, with the Event Entity output naming which one - while the subgraphs follow occupancy, so
/// <see cref="WhileOverlappingPort"/> runs for as long as anything at all is inside.</para>
/// <para>Both source modes poll and diff rather than one of them subscribing to an area's signals. One diff means the
/// two modes report entry and exit identically, and it is what makes an entity with several colliders count once: a
/// hurtbox leaving while a hitbox stays is not an exit, which a signal would have to re-scan the overlap list to know
/// anyway. The cost is that an overlap starting and ending entirely between two polls is not seen.</para>
/// <para>The first poll runs during activation, from <see cref="OnActivated"/> rather than from
/// <see cref="OnActivate"/>. A message emitted while a node is still activating is deferred to the end of it, so
/// polling from there would fire every <see cref="OnEnteredPort"/> at once with the Event Entity output naming only
/// the last of them. <see cref="OnActivated"/> still runs inside the activation, with that deferral already over, so
/// an entity standing inside when the watch begins is reported as it begins, one entity per event.</para>
/// </remarks>
/// <param name="sourceMode">Whether the volume is an area in the scene or a shape the query builds.</param>
/// <param name="includeAreas">Whether overlapping areas count, as well as bodies.</param>
/// <param name="areaPath">Path to the area to watch, from the entity's spatial node. Empty means that node itself.
/// </param>
[StatescriptCategory("Physics")]
public class Overlap3DNode(
	OverlapSourceMode sourceMode = OverlapSourceMode.ExistingArea,
	bool includeAreas = false,
	string areaPath = "") : StateNode<Overlap3DNodeContext>
{
	/// <summary>
	/// Input property index for the entity that owns the watch. Unbound means the ability's owner.
	/// </summary>
	public const byte EntityInput = 0;

	/// <summary>
	/// Input property index for the shape a transient query sweeps.
	/// </summary>
	public const byte ShapeInput = 1;

	/// <summary>
	/// Input property index for where a transient shape sits. Unbound means the entity's own position.
	/// </summary>
	public const byte PositionInput = 2;

	/// <summary>
	/// Input property index for how a transient shape is turned. Unbound leaves it upright.
	/// </summary>
	public const byte RotationInput = 3;

	/// <summary>
	/// Input property index for the collision mask a transient query uses.
	/// </summary>
	public const byte MaskInput = 4;

	/// <summary>
	/// Input property index for how long to wait between polls, in seconds. Unbound polls every fixed update.
	/// </summary>
	public const byte PollIntervalInput = 5;

	/// <summary>
	/// Input property index for the entities left out of the results. Starts as the ability owner.
	/// </summary>
	public const byte IgnoreInput = 6;

	/// <summary>
	/// Output variable index for the entity an entered or exited event is about.
	/// </summary>
	public const byte EventEntityOutput = 0;

	/// <summary>
	/// Output port index for the event emitted once per entity that enters.
	/// </summary>
	public const byte OnEnteredPort = 4;

	/// <summary>
	/// Output port index for the event emitted once per entity that leaves.
	/// </summary>
	public const byte OnExitedPort = 5;

	/// <summary>
	/// Output port index for the subgraph that is active while anything is inside.
	/// </summary>
	public const byte WhileOverlappingPort = 6;

	/// <summary>
	/// Output port index for the subgraph that is active while nothing is inside.
	/// </summary>
	public const byte WhileEmptyPort = 7;

	private readonly OverlapSourceMode _sourceMode = sourceMode;
	private readonly bool _includeAreas = includeAreas;
	private readonly string _areaPath = areaPath ?? string.Empty;

	private bool _reportedMissingSource;

	/// <inheritdoc/>
	public override string Description =>
		"Watches a volume while active, reporting the entities that enter and leave it.";

	/// <inheritdoc/>
	protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
	{
		base.DefinePorts(inputPorts, outputPorts);
		outputPorts.Add(CreatePort<EventPort>(OnEnteredPort, "OnEntered"));
		outputPorts.Add(CreatePort<EventPort>(OnExitedPort, "OnExited"));
		outputPorts.Add(CreatePort<SubgraphPort>(WhileOverlappingPort, "WhileOverlapping"));
		outputPorts.Add(CreatePort<SubgraphPort>(WhileEmptyPort, "WhileEmpty"));
	}

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Entity", typeof(IForgeEntity), IsOptional: true));
		inputProperties.Add(new InputProperty("Shape", typeof(Shape3D), IsOptional: true));

		// Required and seeded with an Entity Position 3D, matching the resolver: an unbound position would have to fall
		// back to the entity, which is the fallback that made the resolver query the world origin.
		inputProperties.Add(new InputProperty("Position", typeof(NumericsVector3)));
		inputProperties.Add(new InputProperty("Rotation", typeof(NumericsQuaternion), IsOptional: true));
		inputProperties.Add(new InputProperty("Mask", typeof(int), IsOptional: true));
		inputProperties.Add(new InputProperty("Poll Interval", typeof(double), IsOptional: true));

		// Not optional, and seeded by the editor with the ability's owner: unbound would mean exactly what an array of
		// the owner already spells. Emptying it is how a watch that reports everyone is authored.
		inputProperties.Add(new InputProperty("Ignore", typeof(IForgeEntity[])));

		outputVariables.Add(new OutputVariable("Event Entity", typeof(IForgeEntity)));
	}

	/// <inheritdoc/>
	protected override void OnActivate(GraphContext graphContext)
	{
		Overlap3DNodeContext nodeContext = graphContext.GetNodeContext<Overlap3DNodeContext>(NodeID);
		nodeContext.Overlapping.Clear();
		nodeContext.Pending.Clear();
		nodeContext.Changed.Clear();
		nodeContext.LastOccupied = null;
		nodeContext.TimeSincePoll = 0;
	}

	/// <inheritdoc/>
	protected override void OnActivated(GraphContext graphContext)
	{
		Poll(graphContext, graphContext.GetNodeContext<Overlap3DNodeContext>(NodeID));
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
		Overlap3DNodeContext nodeContext = graphContext.GetNodeContext<Overlap3DNodeContext>(NodeID);
		nodeContext.Overlapping.Clear();
		nodeContext.Pending.Clear();
		nodeContext.Changed.Clear();

		PhysicsDebugDraw3D.Release(nodeContext.DebugMarker);
		nodeContext.DebugMarker = null;
	}

	/// <inheritdoc/>
	protected override void OnFixedUpdate(double deltaTime, GraphContext graphContext)
	{
		Overlap3DNodeContext nodeContext = graphContext.GetNodeContext<Overlap3DNodeContext>(NodeID);

		graphContext.TryResolve(InputProperties[PollIntervalInput].BoundName, out double pollInterval);

		nodeContext.TimeSincePoll += deltaTime;

		if (nodeContext.TimeSincePoll < pollInterval)
		{
			return;
		}

		nodeContext.TimeSincePoll = 0;
		Poll(graphContext, nodeContext);
	}

	private static void WriteEventEntity(GraphContext graphContext, OutputVariable output, IForgeEntity entity)
	{
		if (output.BoundName == StringKey.Empty)
		{
			return;
		}

		Variables? variables = output.Scope == VariableScope.Shared
			? graphContext.SharedVariables
			: graphContext.GraphVariables;

		if (variables?.TryGetObjectVariableType(output.BoundName, out _) == true)
		{
			variables.SetObject(output.BoundName, entity);
		}
	}

	private void Poll(GraphContext graphContext, Overlap3DNodeContext nodeContext)
	{
		IForgeEntity? entity = ResolveEntityOrOwner(graphContext);

		nodeContext.Pending.Clear();

		if (!TryCollect(graphContext, nodeContext, entity))
		{
			ReportMissingSourceOnce();
		}

		ReportChanges(graphContext, nodeContext, nodeContext.Overlapping, nodeContext.Pending, OnExitedPort);
		ReportChanges(graphContext, nodeContext, nodeContext.Pending, nodeContext.Overlapping, OnEnteredPort);

		bool occupied = nodeContext.Overlapping.Count > 0;

		// Reporting runs the rest of the graph, which may well have ended this node - an entry that kills the target
		// and ends the ability, say. Occupancy belongs to a node that is still watching.
		if (!nodeContext.Active || nodeContext.LastOccupied == occupied)
		{
			return;
		}

		bool hadValue = nodeContext.LastOccupied.HasValue;
		nodeContext.LastOccupied = occupied;

		if (hadValue)
		{
			var previousSubgraphPort =
				(SubgraphPort)OutputPorts[occupied ? WhileEmptyPort : WhileOverlappingPort];
			previousSubgraphPort.EmitDisableSubgraphMessage(graphContext);
		}

		EmitMessage(graphContext, occupied ? WhileOverlappingPort : WhileEmptyPort);
	}

	// Walks `from`, reporting every entity `to` does not have and moving it across. Called once each way round, which
	// is what makes exits and entries the same code: an exit is what the poll no longer has, an entry is what the
	// committed set does not have yet.
	private void ReportChanges(
		GraphContext graphContext,
		Overlap3DNodeContext nodeContext,
		HashSet<IForgeEntity> from,
		HashSet<IForgeEntity> to,
		byte eventPort)
	{
		nodeContext.Changed.Clear();

		foreach (IForgeEntity candidate in from)
		{
			if (!to.Contains(candidate))
			{
				nodeContext.Changed.Add(candidate);
			}
		}

		// Indexed rather than a foreach: each report runs the rest of the graph, which may deactivate this node and
		// clear the list out from under the walk. Re-reading Count each step ends the walk instead of throwing.
		for (int i = 0; i < nodeContext.Changed.Count; i++)
		{
			IForgeEntity changed = nodeContext.Changed[i];

			if (eventPort == OnExitedPort)
			{
				from.Remove(changed);
			}
			else
			{
				to.Add(changed);
			}

			WriteEventEntity(graphContext, OutputVariables[EventEntityOutput], changed);
			EmitMessage(graphContext, eventPort);
		}
	}

	private bool TryCollect(
		GraphContext graphContext,
		Overlap3DNodeContext nodeContext,
		IForgeEntity? entity)
	{
		graphContext.TryResolveObjectArray(
			InputProperties[IgnoreInput].BoundName,
			typeof(IForgeEntity),
			out object?[]? excluded);

		if (_sourceMode == OverlapSourceMode.ExistingArea)
		{
			if (!ForgeEntityBridge.TryGetSpatialNode3D(entity, _areaPath, out Node3D? areaNode)
				|| areaNode is not Area3D area)
			{
				return false;
			}

			PhysicsQuery3D.CollectAreaOverlaps(area, _includeAreas, excluded, nodeContext.Pending);
			return true;
		}

		World3D? world = PhysicsQuery3D.ResolveWorld(graphContext);

		if (world is null
			|| !graphContext.TryResolveObject(InputProperties[ShapeInput].BoundName, out Shape3D? shape)
			|| shape is null
			|| !GodotObject.IsInstanceValid(shape))
		{
			return false;
		}

		graphContext.TryResolve(InputProperties[MaskInput].BoundName, out int mask);

		Transform3D transform = ResolveTransform(graphContext);

		PhysicsQuery3D.CollectShapeOverlaps(
			world,
			shape,
			transform,
			PhysicsQuery3D.ResolveMask(mask),
			_includeAreas,
			excluded,
			nodeContext.Pending);

		// Held for the node's lifetime rather than flashed, so a trap that is armed reads as armed, and recoloured by
		// whether anything is inside, so it reads as triggered without opening a variable. Existing-Area mode draws
		// nothing: Godot already renders the shapes of an area that is in the scene.
		Color markerColor = nodeContext.Pending.Count > 0
			? PhysicsDebugDraw3D.OverlapFoundColor
			: PhysicsDebugDraw3D.OverlapEmptyColor;

		nodeContext.DebugMarker = PhysicsDebugDraw3D.EnsureMarker(graphContext, nodeContext.DebugMarker, markerColor);

		PhysicsDebugDraw3D.SetShape(nodeContext.DebugMarker, shape, transform);

		return true;
	}

	private Transform3D ResolveTransform(GraphContext graphContext)
	{
		graphContext.TryResolve(InputProperties[PositionInput].BoundName, out NumericsVector3 origin);
		var position = new Vector3(origin.X, origin.Y, origin.Z);

		if (!graphContext.TryResolve(InputProperties[RotationInput].BoundName, out NumericsQuaternion rotation))
		{
			return new Transform3D(Basis.Identity, position);
		}

		// A zero quaternion is what an unfilled rotation resolves to, and Godot rejects one outright rather than
		// treating it as no rotation.
		var godotRotation = new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W);

		return godotRotation.LengthSquared() <= 0.000001f
			? new Transform3D(Basis.Identity, position)
			: new Transform3D(new Basis(godotRotation.Normalized()), position);
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

	private void ReportMissingSourceOnce()
	{
		if (_reportedMissingSource)
		{
			return;
		}

		_reportedMissingSource = true;

		if (_sourceMode == OverlapSourceMode.TransientShape)
		{
			GD.PushWarning(
				"Statescript: Overlap 3D has no shape to sweep, or no physics world to sweep it through. " +
				"Nothing is being watched.");
			return;
		}

		GD.PushWarning(
			"Statescript: Overlap 3D found no Area3D for its entity" +
			(_areaPath.Length == 0 ? "." : $" at [{_areaPath}].") +
			" Nothing is being watched.");
	}
}
