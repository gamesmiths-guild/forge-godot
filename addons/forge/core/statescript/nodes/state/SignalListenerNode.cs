// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Core.Statescript.Interop;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Ports;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// State node that connects to a signal on a scene node while active and emits <c>OnSignal</c> each time it fires.
/// </summary>
/// <remarks>
/// <para>The scene-side counterpart of the core Event Listener node, for the things a game already announces through
/// Godot rather than through Forge: a button pressed, an animation's method track, a plate stepped on, a game's own
/// script signalling that a phase changed.</para>
/// <para>It reports the edge and not the payload. Godot matches a connection by arity, so the handler is still shaped
/// like the signal it watches and the arguments are simply discarded - which is what makes this a trigger rather than a
/// data source. When the payload is the point, Overlap 3D reports the entity it found and Forge's own events carry
/// typed data into a graph.</para>
/// <para>The connection is made on activation and taken back down on deactivation, against the node that was connected
/// rather than whatever the input resolves to later, so a resolver that has since moved on cannot leave a live
/// connection behind.</para>
/// </remarks>
/// <param name="signalName">The signal to watch.</param>
/// <param name="oneShot">Whether the node deactivates itself the first time the signal fires.</param>
[StatescriptCategory("Interop")]
public class SignalListenerNode(string signalName = "", bool oneShot = false)
	: InteropStateNodeBase<SignalListenerNodeContext>
{
	/// <summary>
	/// Output port index for the per-emission event.
	/// </summary>
	public const byte OnSignalPort = 4;

	private readonly StringName _signalName = signalName ?? string.Empty;
	private readonly bool _oneShot = oneShot;

	/// <inheritdoc/>
	public override string Description => "Watches a signal on a scene node and emits OnSignal each time it fires.";

	/// <inheritdoc/>
	protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
	{
		base.DefinePorts(inputPorts, outputPorts);
		outputPorts.Add(CreatePort<EventPort>(OnSignalPort, "OnSignal"));
	}

	/// <inheritdoc/>
	protected override void DefineInteropParameters(
		List<InputProperty> inputProperties,
		List<OutputVariable> outputVariables)
	{
	}

	/// <inheritdoc/>
	protected override void OnActivate(GraphContext graphContext)
	{
		SignalListenerNodeContext nodeContext = graphContext.GetNodeContext<SignalListenerNodeContext>(NodeID);
		nodeContext.ConnectedNode = null;

		if (_signalName.IsEmpty)
		{
			WarnOnce("has no signal name, so there is nothing for it to watch.");
			return;
		}

		Node? node = ResolveNode(graphContext);

		if (node is null)
		{
			WarnOnce("resolved no node to watch, and the ability's owner has none either.");
			return;
		}

		int argumentCount = SignalArguments.GetArgumentCount(node, _signalName);

		if (argumentCount < 0)
		{
			WarnOnce($"found no signal [{_signalName}] on [{node.GetPath()}]. Nothing is being watched.");
			return;
		}

		if (!SignalArguments.TryCreateCallable(
			argumentCount,
			() => OnSignalReceived(graphContext, nodeContext),
			out Callable callable))
		{
			WarnOnce(
				$"cannot watch [{_signalName}], which declares {argumentCount} arguments - more than the " +
				$"{SignalArguments.MaxArguments} a handler can be built for.");
			return;
		}

		node.Connect(_signalName, callable);
		nodeContext.ConnectedNode = node;
		nodeContext.ConnectedCallable = callable;
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
		SignalListenerNodeContext nodeContext = graphContext.GetNodeContext<SignalListenerNodeContext>(NodeID);

		if (nodeContext.ConnectedNode is not null
			&& GodotObject.IsInstanceValid(nodeContext.ConnectedNode)
			&& nodeContext.ConnectedNode.IsConnected(_signalName, nodeContext.ConnectedCallable))
		{
			nodeContext.ConnectedNode.Disconnect(_signalName, nodeContext.ConnectedCallable);
		}

		nodeContext.ConnectedNode = null;
		nodeContext.ConnectedCallable = default;
	}

	// The signal fires from wherever Godot emitted it, which is outside the graph's own update, so this re-checks that
	// the node is still active: an earlier emission in the same frame can have ended the ability that owns it. The
	// context is the one captured at activation rather than looked up again, since a graph that has been torn down no
	// longer has one to look up.
	private void OnSignalReceived(GraphContext graphContext, SignalListenerNodeContext nodeContext)
	{
		if (!nodeContext.Active)
		{
			return;
		}

		if (_oneShot)
		{
			DeactivateNodeAndEmitMessage(graphContext, OnSignalPort);
			return;
		}

		EmitMessage(graphContext, OnSignalPort);
	}
}
