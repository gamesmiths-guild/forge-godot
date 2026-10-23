// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Godot;
using ForgeInputActionNode = Gamesmiths.Forge.Statescript.Nodes.State.InputActionNode;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// The core <see cref="ForgeInputActionNode"/>, which also reads the local device when its input source entity has no
/// input of its own.
/// </summary>
/// <remarks>
/// <para>An entity with an <see cref="IForgeEntity.Input"/> is read from that input, once per physics tick, so the node
/// sees exactly what every machine simulating the entity sees, a tap shorter than a tick included. An entity without
/// one is read straight from the local device every frame, which only the machine with the device can do.</para>
/// <para>The action is authored as free text, so it can name an action the project's Input Map does not have, or one
/// the entity's input does not carry. Either is reported once, as a warning.</para>
/// </remarks>
/// <param name="actionName">The input action to watch.</param>
/// <param name="deactivateOnPressed">Whether the node deactivates itself on the first press, for the "wait for one
/// button and move on" shape.</param>
[StatescriptCategory("Input")]
public class InputActionNode(string actionName = "", bool deactivateOnPressed = false)
	: ForgeInputActionNode(actionName ?? string.Empty, deactivateOnPressed)
{
	private readonly string _action = actionName ?? string.Empty;
	private readonly StringName _actionName = actionName ?? string.Empty;

	private bool _reportedMissingAction;
	private bool _checkedAction;

	/// <inheritdoc/>
	protected override void OnActivate(GraphContext graphContext)
	{
		base.OnActivate(graphContext);

		if (ResolveInputSource(graphContext)?.Input is EntityInput input)
		{
			ReportUncarriedAction(input);
		}
		else
		{
			PollDevice(graphContext);
		}
	}

	/// <inheritdoc/>
	protected override void OnFixedUpdate(double deltaTime, GraphContext graphContext)
	{
		base.OnFixedUpdate(deltaTime, graphContext);

		if (!_checkedAction && ResolveInputSource(graphContext)?.Input is EntityInput input)
		{
			ReportUncarriedAction(input);
		}
	}

	/// <inheritdoc/>
	protected override void OnUpdate(double deltaTime, GraphContext graphContext)
	{
		if (ResolveInputSource(graphContext)?.Input is null)
		{
			PollDevice(graphContext);
		}
	}

	// Checked once, on the first read of an input, since the action set never changes.
	private void ReportUncarriedAction(EntityInput input)
	{
		if (_checkedAction)
		{
			return;
		}

		_checkedAction = true;

		if (input.Actions.IndexOf(_action) < 0)
		{
			ReportMissingActionOnce("the entity's input does not carry");
		}
	}

	private void PollDevice(GraphContext graphContext)
	{
		if (!InputMap.HasAction(_actionName))
		{
			ReportMissingActionOnce("the project's Input Map does not have");
			return;
		}

		ReadButtonState(graphContext, Input.IsActionPressed(_actionName));
	}

	// Godot pushes an error of its own for every read of an action that does not exist, which on a node polling each
	// frame is an error per frame.
	private void ReportMissingActionOnce(string where)
	{
		if (_reportedMissingAction)
		{
			return;
		}

		_reportedMissingAction = true;

		GD.PushWarning(
			$"Statescript: Input Action names the input action [{_actionName}], which {where}. Nothing is being " +
			"watched.");
	}
}
