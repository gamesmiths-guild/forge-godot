// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Ports;
using Godot;
using GodotNode = Godot.Node;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// State node that watches the line between two points, reporting when it breaks and when it clears.
/// </summary>
/// <remarks>
/// <para>Composing a Condition Monitor with the Line Of Sight 3D resolver gets the transitions but not what caused
/// them, and reads backwards: the ports come out as "became true" and "became false" for a question nobody phrases
/// that way. This node names them for what they mean and hands back what got in the way, so a channel that breaks can
/// say what broke it.</para>
/// <para>Both ends are points and are re-resolved on every check, so binding them to spatial getters makes the line
/// follow whoever is at either end - a tether between two moving characters, a beam that has to keep its target in
/// view.</para>
/// <para>The ignore input keeps the line off the bodies at its own ends, and it is a list because both ends have that
/// problem: a character's origin sits at its feet, so a line starting there grazes the bottom of its own capsule, and a
/// line drawn to a marker inside someone is stopped by the body that marker belongs to. It starts as an array of the
/// ability's owner and its target, which covers both; emptying it is how a line nothing passes through is authored.
/// </para>
/// </remarks>
/// <param name="deactivateOnBlocked">Whether the node deactivates itself the first time the line breaks, for the
/// "hold this until it is interrupted" shape.</param>
[StatescriptCategory("Physics")]
public class LineOfSight3DNode(bool deactivateOnBlocked = false)
	: StateNode<LineOfSight3DNodeContext>
{
	/// <summary>
	/// Input property index for where the line starts.
	/// </summary>
	public const byte FromInput = 0;

	/// <summary>
	/// Input property index for where the line ends.
	/// </summary>
	public const byte ToInput = 1;

	/// <summary>
	/// Input property index for the entities the line passes through. Starts as the ability owner and its target.
	/// </summary>
	public const byte IgnoreInput = 2;

	/// <summary>
	/// Input property index for the physics layers that block sight.
	/// </summary>
	public const byte MaskInput = 3;

	/// <summary>
	/// Output variable index for the entity that broke the line.
	/// </summary>
	public const byte BlockerEntityOutput = 0;

	/// <summary>
	/// Output variable index for the collider that broke the line.
	/// </summary>
	public const byte BlockerNodeOutput = 1;

	/// <summary>
	/// Output variable index for where the line was broken.
	/// </summary>
	public const byte BlockPositionOutput = 2;

	/// <summary>
	/// Output port index for the event emitted when the line clears.
	/// </summary>
	public const byte OnClearPort = 4;

	/// <summary>
	/// Output port index for the event emitted when the line breaks.
	/// </summary>
	public const byte OnBlockedPort = 5;

	/// <summary>
	/// Output port index for the subgraph that is active while the line is clear.
	/// </summary>
	public const byte WhileClearPort = 6;

	/// <summary>
	/// Output port index for the subgraph that is active while the line is broken.
	/// </summary>
	public const byte WhileBlockedPort = 7;

	private readonly bool _deactivateOnBlocked = deactivateOnBlocked;

	/// <inheritdoc/>
	public override string Description =>
		"Watches the line between two points, emitting transition events and routing clear and blocked subgraphs.";

	/// <inheritdoc/>
	protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
	{
		base.DefinePorts(inputPorts, outputPorts);
		outputPorts.Add(CreatePort<EventPort>(OnClearPort, "OnClear"));
		outputPorts.Add(CreatePort<EventPort>(OnBlockedPort, "OnBlocked"));
		outputPorts.Add(CreatePort<SubgraphPort>(WhileClearPort, "WhileClear"));
		outputPorts.Add(CreatePort<SubgraphPort>(WhileBlockedPort, "WhileBlocked"));
	}

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("From", typeof(NumericsVector3)));
		inputProperties.Add(new InputProperty("To", typeof(NumericsVector3)));

		// Not optional: unbound would mean "ignore the owner and the target", which an array of exactly those two
		// already spells, and the editor must not offer two spellings of one value. The row is seeded with that array
		// instead, so what is on screen is what runs, and emptying it is how a line nothing passes through is authored.
		inputProperties.Add(new InputProperty("Ignore", typeof(IForgeEntity[])));
		inputProperties.Add(new InputProperty("Mask", typeof(int), IsOptional: true));

		outputVariables.Add(new OutputVariable("Blocker Entity", typeof(IForgeEntity)));
		outputVariables.Add(new OutputVariable("Blocker Node", typeof(GodotNode)));
		outputVariables.Add(new OutputVariable("Block Position", typeof(NumericsVector3)));
	}

	/// <inheritdoc/>
	protected override void OnActivate(GraphContext graphContext)
	{
		graphContext.GetNodeContext<LineOfSight3DNodeContext>(NodeID).LastClear = null;
		Check(graphContext);
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
		LineOfSight3DNodeContext nodeContext =
			graphContext.GetNodeContext<LineOfSight3DNodeContext>(NodeID);

		PhysicsDebugDraw3D.Release(nodeContext.DebugMarker);
		nodeContext.DebugMarker = null;
		nodeContext.Exclusions.Clear();
	}

	/// <inheritdoc/>
	protected override void OnFixedUpdate(double deltaTime, GraphContext graphContext)
	{
		Check(graphContext);
	}

	private static Variables? ResolveVariables(GraphContext graphContext, OutputVariable output)
	{
		if (output.BoundName == StringKey.Empty)
		{
			return null;
		}

		return output.Scope == VariableScope.Shared ? graphContext.SharedVariables : graphContext.GraphVariables;
	}

	private static void WriteObject(GraphContext graphContext, OutputVariable output, object? value)
	{
		Variables? variables = ResolveVariables(graphContext, output);

		if (variables?.TryGetObjectVariableType(output.BoundName, out _) == true)
		{
			variables.SetObject(output.BoundName, value);
		}
	}

	private static void WriteVector(GraphContext graphContext, OutputVariable output, NumericsVector3 value)
	{
		Variables? variables = ResolveVariables(graphContext, output);

		if (variables?.TryGetVariant(output.BoundName, out _) == true)
		{
			variables.SetVar(output.BoundName, value);
		}
	}

	private void Check(GraphContext graphContext)
	{
		World3D? world = PhysicsQuery3D.ResolveWorld(graphContext);

		if (world is null
			|| !graphContext.TryResolve(InputProperties[FromInput].BoundName, out NumericsVector3 fromValue)
			|| !graphContext.TryResolve(InputProperties[ToInput].BoundName, out NumericsVector3 toValue))
		{
			return;
		}

		graphContext.TryResolve(InputProperties[MaskInput].BoundName, out int mask);

		LineOfSight3DNodeContext nodeContext =
			graphContext.GetNodeContext<LineOfSight3DNodeContext>(NodeID);

		var from = new Vector3(fromValue.X, fromValue.Y, fromValue.Z);
		var to = new Vector3(toValue.X, toValue.Y, toValue.Z);

		bool hasExclusions =
			PhysicsQuery3D.TryCollectExclusions(
				ResolveIgnored(graphContext),
				nodeContext.Exclusions);

		bool clear = PhysicsQuery3D.TryLineOfSight(
			world,
			from,
			to,
			PhysicsQuery3D.ResolveMask(mask),
			hasExclusions ? nodeContext.Exclusions : null,
			out RaycastResult3D blocker);

		// Written on every check rather than only on a transition: a tether that stays broken still needs to say what
		// is currently in the way, which may not be what broke it.
		WriteBlocker(graphContext, clear, blocker);

		nodeContext.DebugMarker = PhysicsDebugDraw3D.EnsureMarker(
			graphContext,
			nodeContext.DebugMarker,
			clear ? PhysicsDebugDraw3D.SightClearColor : PhysicsDebugDraw3D.SightBlockedColor);

		PhysicsDebugDraw3D.SetLine(nodeContext.DebugMarker, from, clear ? to : blocker.Position);

		if (nodeContext.LastClear == clear)
		{
			return;
		}

		bool hadValue = nodeContext.LastClear.HasValue;
		nodeContext.LastClear = clear;

		// On the transition only, and only what got in the way: the held line is redrawn every check, and a clear line
		// has nobody to name.
		PhysicsDebugDraw3D.FlashTarget(graphContext, blocker.Entity, PhysicsDebugDraw3D.SightBlockedColor);

		if (!clear && _deactivateOnBlocked)
		{
			DeactivateNodeAndEmitMessage(graphContext, OnBlockedPort);
			return;
		}

		if (hadValue)
		{
			var previousSubgraphPort = (SubgraphPort)OutputPorts[clear ? WhileBlockedPort : WhileClearPort];
			previousSubgraphPort.EmitDisableSubgraphMessage(graphContext);
		}

		if (clear)
		{
			EmitMessage(graphContext, OnClearPort, WhileClearPort);
			return;
		}

		EmitMessage(graphContext, OnBlockedPort, WhileBlockedPort);
	}

	private object?[]? ResolveIgnored(GraphContext graphContext)
	{
		StringKey boundName = InputProperties[IgnoreInput].BoundName;

		return boundName != StringKey.Empty
			&& graphContext.TryResolveObjectArray(boundName, typeof(IForgeEntity), out object?[]? entities)
				? entities
				: null;
	}

	private void WriteBlocker(GraphContext graphContext, bool clear, in RaycastResult3D blocker)
	{
		NumericsVector3 blockPosition = clear
			? NumericsVector3.Zero
			: new NumericsVector3(blocker.Position.X, blocker.Position.Y, blocker.Position.Z);

		WriteObject(graphContext, OutputVariables[BlockerEntityOutput], clear ? null : blocker.Entity);
		WriteObject(graphContext, OutputVariables[BlockerNodeOutput], clear ? null : blocker.Node);
		WriteVector(graphContext, OutputVariables[BlockPositionOutput], blockPosition);
	}
}
