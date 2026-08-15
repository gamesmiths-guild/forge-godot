// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Godot.Editor.Attributes;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that reads the evaluated cost of an ability for an attribute, defaulting to the ability driving
/// the graph.
/// </summary>
[Tool]
internal sealed partial class AbilityCostResolverEditor : NodeEditorProperty
{
	private const float LabelWidth = 60.0f;

	private Action? _onChanged;
	private AttributeSelectionControl? _attributePicker;
	private string _selectedSetClass = string.Empty;
	private string _selectedAttribute = string.Empty;
	private NestedResolverPicker? _abilityPicker;

	public override string DisplayName => "Ability Cost";

	public override string ResolverTypeId => "AbilityCost";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(int) || expectedType == typeof(ForgeVariant128);
	}

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		_onChanged = onChanged;

		var existingResource = property?.Resolver as AbilityCostResolverResource;
		_selectedSetClass = existingResource?.AttributeSetClass ?? string.Empty;
		_selectedAttribute = existingResource?.AttributeName ?? string.Empty;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		_attributePicker = new AttributeSelectionControl
		{
			LabelWidth = LabelWidth,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		_attributePicker.ValueChanged += OnAttributeSelectionChanged;
		root.AddChild(_attributePicker);

		_abilityPicker = new NestedResolverPicker();
		_abilityPicker.Initialize(
			graph,
			existingResource?.Ability,
			"Ability:",
			[typeof(AbilityHandle)],
			isArray: false,
			folded: true,
			() => _onChanged?.Invoke(),
			RaiseLayoutSizeChanged,
			IterationScope);
		root.AddChild(_abilityPicker);

		_attributePicker.SetValue(_selectedSetClass, _selectedAttribute);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new AbilityCostResolverResource
		{
			AttributeSetClass = _selectedSetClass,
			AttributeName = _selectedAttribute,
			Ability = _abilityPicker?.BuildResource(),
		};
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_abilityPicker?.ClearCallbacks();
	}

	public override bool TryGetHighlightedVariableName(out string variableName)
	{
		if (_abilityPicker is not null && _abilityPicker.TryGetHighlightedVariableName(out variableName))
		{
			return true;
		}

		variableName = string.Empty;
		return false;
	}

	private void OnAttributeSelectionChanged()
	{
		if (_attributePicker is null)
		{
			return;
		}

		_selectedSetClass = _attributePicker.SetClass;
		_selectedAttribute = _attributePicker.AttributeName;
		_onChanged?.Invoke();
	}
}
#endif
