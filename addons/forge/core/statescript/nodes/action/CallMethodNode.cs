// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Interop;
using Gamesmiths.Forge.Statescript;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;

/// <summary>
/// Action node that calls a method on a scene node.
/// </summary>
/// <remarks>
/// <para>The escape hatch for the things a property cannot express: a door that opens through <c>Open()</c>, a
/// spawner told to <c>SpawnWave(3)</c>, a state machine handed a transition by name. It is also how a graph reaches the
/// methods Godot itself only exposes as calls, which is why Set Node Property cannot stand in for it.</para>
/// <para>Two arguments, filled in order: the second is offered only once the first is set, so a call can never be
/// assembled with a hole in the middle of its argument list. The return value is optional and, when a type is chosen,
/// is written to an output variable.</para>
/// </remarks>
/// <param name="methodName">The method to call.</param>
/// <param name="arg1Type">The type of the first argument, or None for a call that takes none.</param>
/// <param name="arg2Type">The type of the second argument, or None.</param>
/// <param name="returnType">The type of the return value, or None to discard it.</param>
[StatescriptCategory("Interop")]
public sealed class CallMethodNode(
	string methodName = "",
	InteropValueType arg1Type = InteropValueType.None,
	InteropValueType arg2Type = InteropValueType.None,
	InteropValueType returnType = InteropValueType.None) : InteropActionNodeBase
{
	/// <summary>
	/// Input property index for the first argument.
	/// </summary>
	public const byte Argument1Input = 1;

	/// <summary>
	/// Input property index for the second argument.
	/// </summary>
	public const byte Argument2Input = 2;

	/// <summary>
	/// Output variable index for the return value.
	/// </summary>
	public const byte ReturnOutput = 0;

	private readonly StringName _methodName = methodName ?? string.Empty;
	private readonly InteropValueType[] _argumentTypes = [arg1Type, arg2Type];
	private readonly InteropValueType _returnType = returnType;

	/// <inheritdoc/>
	public override string Description => "Calls a method on a scene node.";

	/// <inheritdoc/>
	protected override void DefineInteropParameters(
		List<InputProperty> inputProperties,
		List<OutputVariable> outputVariables)
	{
		// Both argument rows are always declared, whatever their configured type, so a binding keeps its index when an
		// argument is turned off and comes back unchanged when it is turned on again. The editor hides the rows that
		// are not in use.
		inputProperties.Add(new InputProperty("Arg 1", InteropValues.ToClrType(_argumentTypes[0])));
		inputProperties.Add(new InputProperty("Arg 2", InteropValues.ToClrType(_argumentTypes[1])));

		if (_returnType != InteropValueType.None)
		{
			outputVariables.Add(new OutputVariable("Return", InteropValues.ToClrType(_returnType)));
		}
	}

	/// <inheritdoc/>
	protected override void ExecuteOn(Node node, GraphContext graphContext)
	{
		if (_methodName.IsEmpty)
		{
			WarnOnce("has no method name, so there is nothing for it to call.");
			return;
		}

		if (!node.HasMethod(_methodName))
		{
			WarnOnce($"found no method [{_methodName}] on [{node.GetPath()}]. The call was skipped.");
			return;
		}

		Variant returnValue = node.Call(
			_methodName,
			InteropNodeInputs.ResolveArguments(graphContext, InputProperties, Argument1Input, _argumentTypes));

		WriteReturnValue(graphContext, returnValue);
	}

	private void WriteReturnValue(GraphContext graphContext, Variant returnValue)
	{
		if (_returnType == InteropValueType.None)
		{
			return;
		}

		OutputVariable output = OutputVariables[ReturnOutput];

		if (output.BoundName == StringKey.Empty)
		{
			return;
		}

		Variables? variables = output.Scope == VariableScope.Shared
			? graphContext.SharedVariables
			: graphContext.GraphVariables;

		if (variables is null)
		{
			return;
		}

		if (InteropValues.IsObjectLane(_returnType))
		{
			if (variables.TryGetObjectVariableType(output.BoundName, out _))
			{
				variables.SetObject(output.BoundName, InteropValues.ObjectFromGodot(returnValue));
			}

			return;
		}

		// Written as the exact type the output declares, since Variant128 is an untagged union and a mismatch hands
		// the reader whichever bytes happen to overlap. The variable has to exist first: SetVariant throws on an
		// unknown name, and a graph can outlive the variable a shared binding names.
		if (variables.TryGetVariant(output.BoundName, out _))
		{
			variables.SetVariant(output.BoundName, InteropValues.FromGodot(returnValue, _returnType));
		}
	}
}
