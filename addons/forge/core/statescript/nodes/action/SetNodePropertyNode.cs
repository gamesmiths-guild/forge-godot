// Copyright © Gamesmiths Guild.

using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Core.Statescript.Interop;
using Gamesmiths.Forge.Statescript;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;

/// <summary>
/// Action node that writes a value onto a property of a scene node.
/// </summary>
/// <remarks>
/// <para>The direct answer to "Set Variable does not reach the scene". Graph variables are two in-memory bags that
/// nothing outside the graph observes, so a light that should dim, a shader parameter that should follow a charge, or a
/// game's own exported field are all out of reach until something writes to the node itself.</para>
/// <para>The path is a property path rather than a name, so <c>position:y</c> and <c>material:shader_parameter/glow</c>
/// reach into a property as well as at it. The supported value set is closed - see <see cref="InteropValueType"/> - and
/// everything outside it is reported rather than guessed at.</para>
/// <para>The write is permanent, for state that outlives the ability. Node Property Override is the form that puts the
/// old value back.</para>
/// </remarks>
/// <param name="propertyPath">The property to write, as a path from the node.</param>
/// <param name="valueType">The type of the value being written, which has to be the property's own type.</param>
/// <param name="isArray">Whether the property holds an array of that type.</param>
[StatescriptCategory("Interop")]
public sealed class SetNodePropertyNode(
	string propertyPath = "",
	InteropValueType valueType = InteropValueType.Float,
	bool isArray = false) : InteropActionNodeBase
{
	/// <summary>
	/// Input property index for the value to write.
	/// </summary>
	public const byte ValueInput = 1;

	private readonly string _propertyPath = propertyPath ?? string.Empty;
	private readonly InteropValueType _valueType = valueType;
	private readonly bool _isArray = isArray;

	/// <inheritdoc/>
	public override string Description => "Writes a value onto a property of a scene node.";

	/// <inheritdoc/>
	protected override void DefineInteropParameters(
		List<InputProperty> inputProperties,
		List<OutputVariable> outputVariables)
	{
		Type clrType = InteropValues.ToClrType(_valueType);
		inputProperties.Add(new InputProperty("Value", _isArray ? clrType.MakeArrayType() : clrType));
	}

	/// <inheritdoc/>
	protected override void ExecuteOn(Node node, GraphContext graphContext)
	{
		if (_propertyPath.Length == 0)
		{
			WarnOnce("has no property path, so there is nothing for it to write.");
			return;
		}

		var path = new NodePath(_propertyPath);

		if (!NodePropertyAccess.DeclaresProperty(node, path))
		{
			WarnOnce($"found no property [{_propertyPath}] on [{node.GetPath()}]. The write was skipped.");
			return;
		}

		if (!InteropNodeInputs.TryResolveTypedValue(
			graphContext,
			InputProperties[ValueInput].BoundName,
			_valueType,
			_isArray,
			out Variant value))
		{
			WarnOnce($"could not resolve a {_valueType} to write to [{_propertyPath}]. The write was skipped.");
			return;
		}

		node.SetIndexed(path, value);
	}
}
