// Copyright © Gamesmiths Guild.

using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Core.Statescript.Interop;
using Gamesmiths.Forge.Statescript;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// State node that writes a value onto a property of a scene node while active and puts the old one back when it ends.
/// </summary>
/// <remarks>
/// <para>Every deactivation path restores the original value, including an abort, which is what makes it safe to reach
/// into the scene for the length of an ability: a rage buff that doubles a light's energy, a slow that halves a
/// platform's speed, a shader parameter driven for the length of a channel. The permanent form is Set Node Property.
/// </para>
/// <para>The value found at activation is what gets restored, so a scene authored with its own value keeps it, and two
/// overlapping overrides of the same property resolve to whichever ends last rather than to a value neither of them
/// intended.</para>
/// </remarks>
/// <param name="propertyPath">The property to write, as a path from the node.</param>
/// <param name="valueType">The type of the value being written, which has to be the property's own type.</param>
/// <param name="isArray">Whether the property holds an array of that type.</param>
[StatescriptCategory("Interop")]
public class NodePropertyOverrideNode(
	string propertyPath = "",
	InteropValueType valueType = InteropValueType.Float,
	bool isArray = false) : InteropStateNodeBase<NodePropertyOverrideNodeContext>
{
	/// <summary>
	/// Input property index for the value to hold while active.
	/// </summary>
	public const byte ValueInput = 1;

	private readonly string _propertyPath = propertyPath ?? string.Empty;
	private readonly InteropValueType _valueType = valueType;
	private readonly bool _isArray = isArray;

	/// <inheritdoc/>
	public override string Description =>
		"Holds a value on a scene node's property while active, restoring it on deactivation or abort.";

	/// <inheritdoc/>
	protected override void DefineInteropParameters(
		List<InputProperty> inputProperties,
		List<OutputVariable> outputVariables)
	{
		Type clrType = InteropValues.ToClrType(_valueType);
		inputProperties.Add(new InputProperty("Value", _isArray ? clrType.MakeArrayType() : clrType));
	}

	/// <inheritdoc/>
	protected override void OnActivate(GraphContext graphContext)
	{
		NodePropertyOverrideNodeContext nodeContext =
			graphContext.GetNodeContext<NodePropertyOverrideNodeContext>(NodeID);
		nodeContext.Node = null;

		if (_propertyPath.Length == 0)
		{
			WarnOnce("has no property path, so there is nothing for it to hold.");
			return;
		}

		Node? node = ResolveNode(graphContext);

		if (node is null)
		{
			WarnOnce("resolved no node to act on, and the ability's owner has none either.");
			return;
		}

		using var path = new NodePath(_propertyPath);

		if (!NodePropertyAccess.DeclaresProperty(node, path))
		{
			WarnOnce($"found no property [{_propertyPath}] on [{node.GetPath()}]. The override was skipped.");
			return;
		}

		if (!InteropNodeInputs.TryResolveTypedValue(
			graphContext,
			InputProperties[ValueInput].BoundName,
			_valueType,
			_isArray,
			out Variant value))
		{
			WarnOnce($"could not resolve a {_valueType} to write to [{_propertyPath}]. The override was skipped.");
			return;
		}

		nodeContext.Node = node;
		nodeContext.OriginalValue = node.GetIndexed(path);

		node.SetIndexed(path, value);
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
		NodePropertyOverrideNodeContext nodeContext =
			graphContext.GetNodeContext<NodePropertyOverrideNodeContext>(NodeID);
		Node? node = nodeContext.Node;
		nodeContext.Node = null;

		if (node is not null && GodotObject.IsInstanceValid(node))
		{
			using var path = new NodePath(_propertyPath);
			node.SetIndexed(path, nodeContext.OriginalValue);
		}

		nodeContext.OriginalValue = default;
	}

	/// <inheritdoc/>
	protected override void OnUpdate(double deltaTime, GraphContext graphContext)
	{
		// Nothing to do per tick: the override is applied once on activation and undone once on deactivation.
	}
}
