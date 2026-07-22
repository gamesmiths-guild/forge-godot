// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Abilities;
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
	private OptionButton? _setDropdown;
	private OptionButton? _attributeDropdown;
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

		_setDropdown = new SearchableOptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_setDropdown.ItemSelected += OnSetChanged;
		root.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow("Set:", _setDropdown, LabelWidth));

		_attributeDropdown = new SearchableOptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_attributeDropdown.ItemSelected += OnAttributeChanged;
		root.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow("Attr:", _attributeDropdown, LabelWidth));

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

		PopulateSetDropdown();
		PopulateAttributeDropdown();
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

	private void OnSetChanged(long index)
	{
		if (_setDropdown is null)
		{
			return;
		}

		_selectedSetClass = _setDropdown.GetItemText(_setDropdown.Selected);
		_selectedAttribute = string.Empty;
		PopulateAttributeDropdown();
		_onChanged?.Invoke();
	}

	private void OnAttributeChanged(long index)
	{
		if (_attributeDropdown is null)
		{
			return;
		}

		_selectedAttribute = _attributeDropdown.GetItemText(_attributeDropdown.Selected);
		_onChanged?.Invoke();
	}

	private void PopulateSetDropdown()
	{
		if (_setDropdown is null)
		{
			return;
		}

		_setDropdown.Clear();
		foreach (string option in EditorUtils.GetAttributeSetOptions())
		{
			_setDropdown.AddItem(option);
		}

		for (int i = 0; i < _setDropdown.ItemCount; i++)
		{
			if (_setDropdown.GetItemText(i) == _selectedSetClass)
			{
				_setDropdown.Selected = i;
				return;
			}
		}

		if (_setDropdown.ItemCount > 0)
		{
			_setDropdown.Selected = 0;
			_selectedSetClass = _setDropdown.GetItemText(0);
		}
	}

	private void PopulateAttributeDropdown()
	{
		if (_attributeDropdown is null)
		{
			return;
		}

		_attributeDropdown.Clear();
		foreach (string option in EditorUtils.GetAttributeOptions(_selectedSetClass))
		{
			_attributeDropdown.AddItem(option);
		}

		for (int i = 0; i < _attributeDropdown.ItemCount; i++)
		{
			if (_attributeDropdown.GetItemText(i) == _selectedAttribute)
			{
				_attributeDropdown.Selected = i;
				return;
			}
		}

		if (_attributeDropdown.ItemCount > 0)
		{
			_attributeDropdown.Selected = 0;
			_selectedAttribute = _attributeDropdown.GetItemText(0);
		}
	}
}
#endif
