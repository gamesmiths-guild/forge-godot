// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Ports;
using Godot;
using GodotNode = Godot.Node;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// State node that watches the line between two points, reporting when it breaks and when it clears.
/// </summary>
/// <remarks>
/// <para>Composing a Condition Monitor with the Line Of Sight 2D resolver gets the transitions but not what caused
/// them, and reads backwards: the ports come out as "became true" and "became false" for a question nobody phrases
/// that way. This node names them for what they mean and hands back what got in the way, so a channel that breaks can
/// say what broke it.</para>
/// <para>Both ends are points and are re-resolved on every check, so binding them to spatial getters makes the line
/// follow whoever is at either end - a tether between two moving characters, a beam that has to keep its target in
/// view.</para>
/// <para>The ignore input keeps the line off the bodies at its own ends, and it is a list because both ends have that
/// problem: a line starting at a character's own origin starts inside that character's own collider, and a line drawn
/// to a marker inside someone is stopped by the body that marker belongs to. It starts as an array of the ability's
/// owner and its target, which covers both; emptying it is how a line nothing passes through is authored.</para>
/// </remarks>
/// <param name="deactivateOnBlocked">Whether the node deactivates itself the first time the line breaks, for the
/// "hold this until it is interrupted" shape.</param>
[StatescriptCategory("Physics")]
public class LineOfSight2DNode(bool deactivateOnBlocked = false)
	: StateNode<LineOfSight2DNodeContext>
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
		inputProperties.Add(new InputProperty("From", typeof(NumericsVector2)));
		inputProperties.Add(new InputProperty("To", typeof(NumericsVector2)));

		// Not optional: unbound would mean "ignore the owner and the target", which an array of exactly those two
		// already spells, and the editor must not offer two spellings of one value. The row is seeded with that array
		// instead, so what is on screen is what runs, and emptying it is how a line nothing passes through is authored.
		inputProperties.Add(new InputProperty("Ignore", typeof(IForgeEntity[])));
		inputProperties.Add(new InputProperty("Mask", typeof(int), IsOptional: true));

		outputVariables.Add(new OutputVariable("Blocker Entity", typeof(IForgeEntity)));
		outputVariables.Add(new OutputVariable("Blocker Node", typeof(GodotNode)));
		outputVariables.Add(new OutputVariable("Block Position", typeof(NumericsVector2)));
	}

	/// <inheritdoc/>
	protected override void OnActivate(GraphContext graphContext)
	{
		graphContext.GetNodeContext<LineOfSight2DNodeContext>(NodeID).LastClear = null;
		Check(graphContext);
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
		LineOfSight2DNodeContext nodeContext =
			graphContext.GetNodeContext<LineOfSight2DNodeContext>(NodeID);

		PhysicsDebugDraw2D.Release(nodeContext.DebugMarker);
		nodeContext.DebugMarker = null;
		nodeContext.Exclusions.Clear();
	}

	/// <inheritdoc/>
	protected override void OnUpdate(double deltaTime, GraphContext graphContext)
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

	private static void WriteVector(GraphContext graphContext, OutputVariable output, NumericsVector2 value)
	{
		Variables? variables = ResolveVariables(graphContext, output);

		if (variables?.TryGetVariant(output.BoundName, out _) == true)
		{
			variables.SetVar(output.BoundName, value);
		}
	}

	private void Check(GraphContext graphContext)
	{
		World2D? world = PhysicsQuery2D.ResolveWorld(graphContext);

		if (world is null
			|| !graphContext.TryResolve(InputProperties[FromInput].BoundName, out NumericsVector2 fromValue)
			|| !graphContext.TryResolve(InputProperties[ToInput].BoundName, out NumericsVector2 toValue))
		{
			return;
		}

		graphContext.TryResolve(InputProperties[MaskInput].BoundName, out int mask);

		LineOfSight2DNodeContext nodeContext =
			graphContext.GetNodeContext<LineOfSight2DNodeContext>(NodeID);

		var from = new Vector2(fromValue.X, fromValue.Y);
		var to = new Vector2(toValue.X, toValue.Y);

		bool hasExclusions =
			PhysicsQuery2D.TryCollectExclusions(
				ResolveIgnored(graphContext),
				nodeContext.Exclusions);

		bool clear = PhysicsQuery2D.TryLineOfSight(
			world,
			from,
			to,
			PhysicsQuery2D.ResolveMask(mask),
			hasExclusions ? nodeContext.Exclusions : null,
			out RaycastResult2D blocker);

		// Written on every check rather than only on a transition: a tether that stays broken still needs to say what
		// is currently in the way, which may not be what broke it.
		WriteBlocker(graphContext, clear, blocker);

		nodeContext.DebugMarker = PhysicsDebugDraw2D.EnsureMarker(
			graphContext,
			nodeContext.DebugMarker,
			clear ? PhysicsDebugDraw2D.SightClearColor : PhysicsDebugDraw2D.SightBlockedColor);

		PhysicsDebugDraw2D.SetLine(nodeContext.DebugMarker, from, clear ? to : blocker.Position);

		if (nodeContext.LastClear == clear)
		{
			return;
		}

		bool hadValue = nodeContext.LastClear.HasValue;
		nodeContext.LastClear = clear;

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

	private void WriteBlocker(GraphContext graphContext, bool clear, in RaycastResult2D blocker)
	{
		NumericsVector2 blockPosition = clear
			? NumericsVector2.Zero
			: new NumericsVector2(blocker.Position.X, blocker.Position.Y);

		WriteObject(graphContext, OutputVariables[BlockerEntityOutput], clear ? null : blocker.Entity);
		WriteObject(graphContext, OutputVariables[BlockerNodeOutput], clear ? null : blocker.Node);
		WriteVector(graphContext, OutputVariables[BlockPositionOutput], blockPosition);
	}
}
