// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that reads a state flag from an ability, defaulting to the ability driving the graph.
/// </summary>
[Tool]
internal sealed partial class AbilityStateResolverEditor : NodeEditorProperty
{
	private const float LabelWidth = 60.0f;

	private Action? _onChanged;
	private AbilityStateType _stateType;
	private NestedResolverPicker? _abilityPicker;

	public override string DisplayName => "Ability State";

	public override string ResolverTypeId => "AbilityState";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(bool) || expectedType == typeof(ForgeVariant128);
	}

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		_onChanged = onChanged;

		var existingResource = property?.Resolver as AbilityStateResolverResource;
		_stateType = existingResource?.StateType ?? AbilityStateType.IsActive;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		var stateDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		foreach (AbilityStateType value in Enum.GetValues<AbilityStateType>())
		{
			stateDropdown.AddItem(value.ToString());
		}

		stateDropdown.Selected = (int)_stateType;
		stateDropdown.ItemSelected += index =>
		{
			_stateType = (AbilityStateType)(int)index;
			_onChanged?.Invoke();
		};
		root.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow("State:", stateDropdown, LabelWidth));

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
		property.Resolver = new AbilityStateResolverResource
		{
			StateType = _stateType,
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
