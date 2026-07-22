// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Linq;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Editor.Tags;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that reads a cooldown value from an ability, defaulting to the ability driving the graph.
/// </summary>
[Tool]
internal sealed partial class AbilityCooldownResolverEditor : NodeEditorProperty
{
	private const float LabelWidth = 60.0f;

	private Action? _onChanged;
	private AbilityCooldownDataType _dataType;
	private string _cooldownTag = string.Empty;
	private TagContainerSelectionControl? _tagControl;
	private NestedResolverPicker? _abilityPicker;

	public override string DisplayName => "Ability Cooldown";

	public override string ResolverTypeId => "AbilityCooldown";

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

		var existingResource = property?.Resolver as AbilityCooldownResolverResource;
		_dataType = existingResource?.DataType ?? AbilityCooldownDataType.RemainingTime;
		_cooldownTag = existingResource?.CooldownTag ?? string.Empty;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		var dataDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		foreach (AbilityCooldownDataType value in Enum.GetValues<AbilityCooldownDataType>())
		{
			dataDropdown.AddItem(value.ToString());
		}

		dataDropdown.Selected = (int)_dataType;
		dataDropdown.ItemSelected += index =>
		{
			_dataType = (AbilityCooldownDataType)(int)index;
			_onChanged?.Invoke();
		};
		root.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow("Data:", dataDropdown, LabelWidth));

		_tagControl = new TagContainerSelectionControl { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		root.AddChild(_tagControl);
		_tagControl.SetValue(
			string.IsNullOrEmpty(_cooldownTag) ? [] : [_cooldownTag]);
		_tagControl.ValueChanged += tags =>
		{
			_cooldownTag = tags.FirstOrDefault() ?? string.Empty;
			_onChanged?.Invoke();
		};

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
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new AbilityCooldownResolverResource
		{
			DataType = _dataType,
			CooldownTag = _cooldownTag,
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
}
#endif
