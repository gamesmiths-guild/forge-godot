// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Ports;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// State node that watches a button while active, reporting when it is pressed and released.
/// </summary>
/// <remarks>
/// <para>This is the "wait for a button before continuing" node. <see cref="WhilePressedPort"/> gives hold-to-channel,
/// <see cref="OnReleasedPort"/> with a timer gives a charged shot, and
/// <paramref name="deactivateOnPressed"/> inside a timer's subgraph gives a combo window.</para>
/// <para>The events are edges the node itself saw, so a button already held when the node activates is not a press:
/// a combo window must not fire on a button nobody pressed inside it, and the button that started the ability is
/// usually still down when the window opens. The subgraph is the other half of that rule - it follows the button's
/// state rather than its edges, so an ability activated by the very button it channels on starts channelling at once.
/// </para>
/// <para>Input is read straight from the device, which makes this client-local. An authoritative or networked game
/// should sample button state once at activation into the ability's activation data rather than polling it inside a
/// graph the server runs.</para>
/// </remarks>
/// <param name="actionName">The input action to watch.</param>
/// <param name="deactivateOnPressed">Whether the node deactivates itself on the first press, for the "wait for one
/// button and move on" shape.</param>
[StatescriptCategory("Input")]
public class InputActionNode(string actionName = "", bool deactivateOnPressed = false)
	: StateNode<InputActionNodeContext>
{
	/// <summary>
	/// Output port index for the event emitted when the button goes down.
	/// </summary>
	public const byte OnPressedPort = 4;

	/// <summary>
	/// Output port index for the event emitted when the button comes up.
	/// </summary>
	public const byte OnReleasedPort = 5;

	/// <summary>
	/// Output port index for the subgraph that is active while the button is down.
	/// </summary>
	public const byte WhilePressedPort = 6;

	private readonly string _actionName = actionName ?? string.Empty;
	private readonly bool _deactivateOnPressed = deactivateOnPressed;

	private bool _reportedMissingAction;

	/// <inheritdoc/>
	public override string Description =>
		"Watches a button while active, emitting press and release events and routing a held subgraph.";

	/// <inheritdoc/>
	protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
	{
		base.DefinePorts(inputPorts, outputPorts);
		outputPorts.Add(CreatePort<EventPort>(OnPressedPort, "OnPressed"));
		outputPorts.Add(CreatePort<EventPort>(OnReleasedPort, "OnReleased"));
		outputPorts.Add(CreatePort<SubgraphPort>(WhilePressedPort, "WhilePressed"));
	}

	/// <inheritdoc/>
	protected override void OnActivate(GraphContext graphContext)
	{
		graphContext.GetNodeContext<InputActionNodeContext>(NodeID).LastPressed = null;
		Poll(graphContext);
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
		graphContext.GetNodeContext<InputActionNodeContext>(NodeID).LastPressed = null;
	}

	/// <inheritdoc/>
	protected override void OnUpdate(double deltaTime, GraphContext graphContext)
	{
		Poll(graphContext);
	}

	private void Poll(GraphContext graphContext)
	{
		if (!InputMap.HasAction(_actionName))
		{
			ReportMissingActionOnce();
			return;
		}

		InputActionNodeContext nodeContext = graphContext.GetNodeContext<InputActionNodeContext>(NodeID);
		bool pressed = Input.IsActionPressed(_actionName);

		if (nodeContext.LastPressed == pressed)
		{
			return;
		}

		bool hadValue = nodeContext.LastPressed.HasValue;
		nodeContext.LastPressed = pressed;

		if (!hadValue)
		{
			// The first read is the state the node started in rather than a change, so only the subgraph follows it.
			if (pressed)
			{
				EmitMessage(graphContext, WhilePressedPort);
			}

			return;
		}

		if (!pressed)
		{
			var whilePressedPort = (SubgraphPort)OutputPorts[WhilePressedPort];
			whilePressedPort.EmitDisableSubgraphMessage(graphContext);
			EmitMessage(graphContext, OnReleasedPort);
			return;
		}

		if (_deactivateOnPressed)
		{
			DeactivateNodeAndEmitMessage(graphContext, OnPressedPort);
			return;
		}

		EmitMessage(graphContext, OnPressedPort, WhilePressedPort);
	}

	// Godot pushes an error of its own for every read of an action that does not exist, which on a node polling each
	// frame is an error per frame.
	private void ReportMissingActionOnce()
	{
		if (_reportedMissingAction)
		{
			return;
		}

		_reportedMissingAction = true;

		GD.PushWarning(
			$"Statescript: Input Action names the input action [{_actionName}], which the project's Input Map does " +
			"not have. Nothing is being watched.");
	}
}
