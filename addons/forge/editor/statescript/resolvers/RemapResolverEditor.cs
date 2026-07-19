// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class RemapResolverEditor : NodeEditorProperty
{
	private static readonly Type[] _operandExpectedTypes = [typeof(int), typeof(float), typeof(double)];

	private Action? _onChanged;
	private NestedResolverPicker? _valuePicker;
	private NestedResolverPicker? _inMinPicker;
	private NestedResolverPicker? _inMaxPicker;
	private NestedResolverPicker? _outMinPicker;
	private NestedResolverPicker? _outMaxPicker;
	private bool _clamp;

	public override string DisplayName => "Remap";

	public override string ResolverTypeId => "Remap";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(float)
			|| expectedType == typeof(double)
			|| expectedType == typeof(ForgeVariant128);
	}

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		_onChanged = onChanged;

		var existingResource = property?.Resolver as RemapResolverResource;
		_clamp = existingResource?.Clamp ?? false;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		_valuePicker = CreatePicker(root, graph, existingResource?.Value, "Value:");
		_inMinPicker = CreatePicker(root, graph, existingResource?.InMin, "In Min:");
		_inMaxPicker = CreatePicker(root, graph, existingResource?.InMax, "In Max:");
		_outMinPicker = CreatePicker(root, graph, existingResource?.OutMin, "Out Min:");
		_outMaxPicker = CreatePicker(root, graph, existingResource?.OutMax, "Out Max:");

		var clampCheck = new CheckBox { Text = "Clamp", ButtonPressed = _clamp };
		clampCheck.Toggled += pressed =>
		{
			_clamp = pressed;
			_onChanged?.Invoke();
		};
		root.AddChild(clampCheck);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new RemapResolverResource
		{
			Value = _valuePicker?.BuildResource(),
			InMin = _inMinPicker?.BuildResource(),
			InMax = _inMaxPicker?.BuildResource(),
			OutMin = _outMinPicker?.BuildResource(),
			OutMax = _outMaxPicker?.BuildResource(),
			Clamp = _clamp,
		};
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_valuePicker?.ClearCallbacks();
		_inMinPicker?.ClearCallbacks();
		_inMaxPicker?.ClearCallbacks();
		_outMinPicker?.ClearCallbacks();
		_outMaxPicker?.ClearCallbacks();
	}

	public override bool TryGetHighlightedVariableName(out string variableName)
	{
		foreach (NestedResolverPicker? picker in Pickers())
		{
			if (picker is not null && picker.TryGetHighlightedVariableName(out variableName))
			{
				return true;
			}
		}

		variableName = string.Empty;
		return false;
	}

	private NestedResolverPicker?[] Pickers()
	{
		return [_valuePicker, _inMinPicker, _inMaxPicker, _outMinPicker, _outMaxPicker];
	}

	private NestedResolverPicker CreatePicker(
		VBoxContainer root,
		StatescriptGraph graph,
		StatescriptResolverResource? existingNested,
		string title)
	{
		var picker = new NestedResolverPicker();
		picker.Initialize(
			graph,
			existingNested,
			title,
			_operandExpectedTypes,
			isArray: false,
			folded: true,
			() => _onChanged?.Invoke(),
			RaiseLayoutSizeChanged,
			IterationScope);
		root.AddChild(picker);
		return picker;
	}
}
#endif
