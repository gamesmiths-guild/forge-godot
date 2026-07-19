// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor for the ternary select: a boolean condition picks between two branches of the input's expected
/// type — including object-backed types like entities.
/// </summary>
[Tool]
internal sealed partial class ConditionalResolverEditor : NodeEditorProperty
{
	private Action? _onChanged;
	private NestedResolverPicker? _conditionPicker;
	private NestedResolverPicker? _whenTruePicker;
	private NestedResolverPicker? _whenFalsePicker;

	public override string DisplayName => "Conditional";

	public override string ResolverTypeId => "Conditional";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType != typeof(ForgeVariant128[]);
	}

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		_onChanged = onChanged;

		var existingResource = property?.Resolver as ConditionalResolverResource;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		Type branchExpectedType = expectedType == typeof(ForgeVariant128) ? typeof(double) : expectedType;

		_conditionPicker = CreatePicker(
			root,
			graph,
			existingResource?.Condition,
			"If:",
			[typeof(bool)],
			existingResource?.ConditionFolded ?? false);
		_whenTruePicker = CreatePicker(
			root,
			graph,
			existingResource?.WhenTrue,
			"Then:",
			[branchExpectedType],
			existingResource?.WhenTrueFolded ?? false);
		_whenFalsePicker = CreatePicker(
			root,
			graph,
			existingResource?.WhenFalse,
			"Else:",
			[branchExpectedType],
			existingResource?.WhenFalseFolded ?? false);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new ConditionalResolverResource
		{
			Condition = _conditionPicker?.BuildResource(),
			WhenTrue = _whenTruePicker?.BuildResource(),
			WhenFalse = _whenFalsePicker?.BuildResource(),
			ConditionFolded = _conditionPicker?.Folded ?? false,
			WhenTrueFolded = _whenTruePicker?.Folded ?? false,
			WhenFalseFolded = _whenFalsePicker?.Folded ?? false,
		};
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_conditionPicker?.ClearCallbacks();
		_whenTruePicker?.ClearCallbacks();
		_whenFalsePicker?.ClearCallbacks();
	}

	public override bool TryGetHighlightedVariableName(out string variableName)
	{
		foreach (NestedResolverPicker? picker in new[] { _conditionPicker, _whenTruePicker, _whenFalsePicker })
		{
			if (picker is not null && picker.TryGetHighlightedVariableName(out variableName))
			{
				return true;
			}
		}

		variableName = string.Empty;
		return false;
	}

	private NestedResolverPicker CreatePicker(
		VBoxContainer root,
		StatescriptGraph graph,
		StatescriptResolverResource? existingNested,
		string title,
		Type[] expectedTypes,
		bool folded)
	{
		var picker = new NestedResolverPicker();
		picker.Initialize(
			graph,
			existingNested,
			title,
			expectedTypes,
			isArray: false,
			folded,
			() => _onChanged?.Invoke(),
			RaiseLayoutSizeChanged,
			IterationScope);
		root.AddChild(picker);
		return picker;
	}
}
#endif
