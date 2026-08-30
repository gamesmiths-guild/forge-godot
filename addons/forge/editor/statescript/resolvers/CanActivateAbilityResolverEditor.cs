// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that checks whether an ability can currently activate, defaulting to the ability driving the
/// graph.
/// </summary>
[Tool]
internal sealed partial class CanActivateAbilityResolverEditor : NodeEditorProperty
{
	private Action? _onChanged;
	private NestedResolverPicker? _targetPicker;
	private NestedResolverPicker? _abilityPicker;

	public override string DisplayName => "Can Activate Ability";

	public override string ResolverTypeId => "CanActivateAbility";

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

		var existingResource = property?.Resolver as CanActivateAbilityResolverResource;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		_targetPicker = new NestedResolverPicker();
		_targetPicker.Initialize(
			graph,
			existingResource?.Target,
			"Target:",
			[typeof(IForgeEntity)],
			isArray: false,
			folded: true,
			() => _onChanged?.Invoke(),
			RaiseLayoutSizeChanged,
			IterationScope);
		root.AddChild(_targetPicker);

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
		property.Resolver = new CanActivateAbilityResolverResource
		{
			Target = _targetPicker?.BuildResource(),
			Ability = _abilityPicker?.BuildResource(),
		};
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_targetPicker?.ClearCallbacks();
		_abilityPicker?.ClearCallbacks();
	}

	public override bool TryGetHighlightedVariableName(out string variableName)
	{
		if (_targetPicker is not null && _targetPicker.TryGetHighlightedVariableName(out variableName))
		{
			return true;
		}

		if (_abilityPicker is not null && _abilityPicker.TryGetHighlightedVariableName(out variableName))
		{
			return true;
		}

		variableName = string.Empty;
		return false;
	}
}
#endif
