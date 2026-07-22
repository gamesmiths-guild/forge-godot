// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that reads a selected value (remaining duration, stacks, level, ...) from an active effect
/// handle.
/// </summary>
[Tool]
internal sealed partial class ActiveEffectDataResolverEditor : NodeEditorProperty
{
	private const float LabelWidth = 60.0f;

	private Action? _onChanged;
	private ActiveEffectDataType _dataType;
	private NestedResolverPicker? _handlePicker;

	public override string DisplayName => "Active Effect Data";

	public override string ResolverTypeId => "ActiveEffectData";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(double)
			|| expectedType == typeof(int)
			|| expectedType == typeof(bool)
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

		var existingResource = property?.Resolver as ActiveEffectDataResolverResource;
		_dataType = existingResource?.DataType ?? ActiveEffectDataType.RemainingDuration;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		var dataDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		foreach (ActiveEffectDataType value in Enum.GetValues<ActiveEffectDataType>())
		{
			dataDropdown.AddItem(value.ToString());
		}

		dataDropdown.Selected = (int)_dataType;
		dataDropdown.ItemSelected += index =>
		{
			_dataType = (ActiveEffectDataType)(int)index;
			_onChanged?.Invoke();
		};
		root.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow("Data:", dataDropdown, LabelWidth));

		_handlePicker = new NestedResolverPicker();
		_handlePicker.Initialize(
			graph,
			existingResource?.ActiveEffect,
			"Effect:",
			[typeof(ActiveEffectHandle)],
			isArray: false,
			folded: false,
			() => _onChanged?.Invoke(),
			RaiseLayoutSizeChanged,
			IterationScope);
		root.AddChild(_handlePicker);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new ActiveEffectDataResolverResource
		{
			DataType = _dataType,
			ActiveEffect = _handlePicker?.BuildResource(),
		};
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_handlePicker?.ClearCallbacks();
	}

	public override bool TryGetHighlightedVariableName(out string variableName)
	{
		if (_handlePicker is not null && _handlePicker.TryGetHighlightedVariableName(out variableName))
		{
			return true;
		}

		variableName = string.Empty;
		return false;
	}
}
#endif
