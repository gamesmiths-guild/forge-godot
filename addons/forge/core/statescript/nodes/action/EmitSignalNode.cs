// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Core.Statescript.Interop;
using Gamesmiths.Forge.Statescript;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;

/// <summary>
/// Action node that emits a signal on a scene node.
/// </summary>
/// <remarks>
/// <para>How a graph tells the rest of the game something happened without knowing who is listening: a HUD element
/// connected to the player's <c>dashed</c> signal, a door announcing that it opened, an achievement watcher. Forge's
/// own event bus is the right channel between graphs and effects; this is the channel to the scene, where the listener
/// is a Godot node connected in the editor.</para>
/// <para>Two arguments, filled in order and matching the signal's own declaration, so a signal that carries a payload
/// can be emitted with one. A count that does not match what the signal declares is reported rather than emitted,
/// because Godot answers a mismatch with an error per emission.</para>
/// </remarks>
/// <param name="signalName">The signal to emit.</param>
/// <param name="arg1Type">The type of the first argument, or None for a signal that carries none.</param>
/// <param name="arg2Type">The type of the second argument, or None.</param>
[StatescriptCategory("Interop")]
public sealed class EmitSignalNode(
	string signalName = "",
	InteropValueType arg1Type = InteropValueType.None,
	InteropValueType arg2Type = InteropValueType.None) : InteropActionNodeBase
{
	/// <summary>
	/// Input property index for the first argument.
	/// </summary>
	public const byte Argument1Input = 1;

	/// <summary>
	/// Input property index for the second argument.
	/// </summary>
	public const byte Argument2Input = 2;

	private readonly StringName _signalName = signalName ?? string.Empty;
	private readonly InteropValueType[] _argumentTypes = [arg1Type, arg2Type];

	/// <inheritdoc/>
	public override string Description => "Emits a signal on a scene node.";

	/// <inheritdoc/>
	protected override void DefineInteropParameters(
		List<InputProperty> inputProperties,
		List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Arg 1", InteropValues.ToClrType(_argumentTypes[0])));
		inputProperties.Add(new InputProperty("Arg 2", InteropValues.ToClrType(_argumentTypes[1])));
	}

	/// <inheritdoc/>
	protected override void ExecuteOn(Node node, GraphContext graphContext)
	{
		if (_signalName.IsEmpty)
		{
			WarnOnce("has no signal name, so there is nothing for it to emit.");
			return;
		}

		if (!node.HasSignal(_signalName))
		{
			WarnOnce($"found no signal [{_signalName}] on [{node.GetPath()}]. Nothing was emitted.");
			return;
		}

		Variant[] arguments = InteropNodeInputs.ResolveArguments(
			graphContext,
			InputProperties,
			Argument1Input,
			_argumentTypes);

		int declaredCount = SignalArguments.GetArgumentCount(node, _signalName);

		if (declaredCount != arguments.Length)
		{
			WarnOnce(
				$"passes {arguments.Length} argument(s) to [{_signalName}], which declares {declaredCount}. " +
				"Nothing was emitted.");
			return;
		}

		node.EmitSignal(_signalName, arguments);
	}
}
