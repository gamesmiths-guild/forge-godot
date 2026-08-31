// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Linq;
using Gamesmiths.Forge.Godot.Core.Statescript.Interop;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;
using GodotNode = Godot.Node;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that reads a property off a scene node.
/// </summary>
/// <remarks>
/// The type row is seeded from the slot the resolver sits in, so a Vector3 input already reads a Vector3 and only an
/// operand that accepts anything - the sides of a comparison, a math operand - has a choice left to make.
/// </remarks>
[Tool]
internal sealed partial class NodePropertyResolverEditor : NodeEditorProperty
{
	private static readonly Type[] _nodeExpectedTypes = [typeof(GodotNode)];

	private Action? _onChanged;
	private NestedResolverPicker? _nodePicker;
	private LineEdit? _propertyPathField;
	private OptionButton? _valueTypeDropdown;
	private InteropValueType[] _valueTypes = [];
	private bool _isArray;

	public override string DisplayName => "Node Property";

	public override string ResolverTypeId => "NodeProperty";

	public override bool SupportsArrayValues => true;

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(ForgeVariant128) || InteropValues.TryFromClrType(expectedType, out _);
	}

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		var resource = property?.Resolver as NodePropertyResolverResource;
		_onChanged = onChanged;
		_isArray = isArray;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		// Seeded with Node From Entity: a nested operand has no unbound state, so an unseeded node would resolve to
		// nothing and every read would return a default. "A property on me" is what a fresh one now starts as.
		_nodePicker = new NestedResolverPicker();
		_nodePicker.Initialize(
			graph,
			resource?.Node ?? new NodeFromEntityResolverResource(),
			"Node:",
			_nodeExpectedTypes,
			isArray: false,
			resource?.NodeFolded ?? false,
			() => _onChanged?.Invoke(),
			RaiseLayoutSizeChanged,
			IterationScope);

		root.AddChild(_nodePicker);

		_propertyPathField = new LineEdit
		{
			PlaceholderText = "visible",
			Text = resource?.PropertyPath ?? string.Empty,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			TooltipText = "A property path from the node. Sub-properties work: position:y, "
				+ "material:shader_parameter/glow.",
		};

		_propertyPathField.TextChanged += _ => _onChanged?.Invoke();

		root.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow(
			"Property:",
			_propertyPathField,
			ResolverEditorLayoutUtilities.SettingLabelWidth));

		_valueTypes = GetSelectableValueTypes(expectedType);

		_valueTypeDropdown = ResolverEditorLayoutUtilities.CreateEnumRow(
			root,
			"Type:",
			[.. _valueTypes.Select(x => x.ToString())],
			Array.IndexOf(_valueTypes, ResolveInitialValueType(resource, expectedType)),
			() => _onChanged?.Invoke());
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new NodePropertyResolverResource
		{
			Node = _nodePicker?.BuildResource(),
			NodeFolded = _nodePicker?.Folded ?? false,
			PropertyPath = _propertyPathField is not null && IsInstanceValid(_propertyPathField)
				? _propertyPathField.Text
				: string.Empty,
			ValueType = SelectedValueType(),
			IsArray = _isArray,
		};
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = _propertyPathField is not null
			&& IsInstanceValid(_propertyPathField)
			&& _propertyPathField.Text.Length > 0
				? _propertyPathField.Text
				: "Node Property";

		return true;
	}

	public override bool TryGetHighlightedVariableName(out string variableName)
	{
		if (_nodePicker is not null && _nodePicker.TryGetHighlightedVariableName(out variableName))
		{
			return true;
		}

		variableName = string.Empty;
		return false;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_nodePicker?.ClearCallbacks();
		_nodePicker = null;
		_propertyPathField = null;
		_valueTypeDropdown = null;
	}

	private static InteropValueType[] GetSelectableValueTypes(Type expectedType)
	{
		if (expectedType == typeof(GodotNode))
		{
			return [InteropValueType.Node];
		}

		InteropValueType[] valueTypes = [.. Enum.GetValues<InteropValueType>()
			.Where(x => x is not InteropValueType.None and not InteropValueType.Node)];

		if (expectedType == typeof(ForgeVariant128))
		{
			return valueTypes;
		}

		return [.. valueTypes.Where(x =>
			StatescriptVariableTypeConverter.IsCompatible(expectedType, InteropValues.ToVariableType(x)))];
	}

	private InteropValueType ResolveInitialValueType(NodePropertyResolverResource? resource, Type expectedType)
	{
		if (resource is not null && Array.IndexOf(_valueTypes, resource.ValueType) >= 0)
		{
			return resource.ValueType;
		}

		return InteropValues.TryFromClrType(expectedType, out InteropValueType valueType)
			&& Array.IndexOf(_valueTypes, valueType) >= 0
				? valueType
				: _valueTypes[0];
	}

	private InteropValueType SelectedValueType()
	{
		return _valueTypeDropdown is not null && IsInstanceValid(_valueTypeDropdown) && _valueTypes.Length > 0
			? _valueTypes[Math.Clamp(_valueTypeDropdown.Selected, 0, _valueTypes.Length - 1)]
			: InteropValueType.Float;
	}
}
#endif
