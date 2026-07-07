// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class CountResolverEditor : ArrayReductionResolverEditorBase
{
	private NestedResolverPicker? _predicatePicker;
	private bool _usePredicate;

	public override string DisplayName => "Count";

	public override string ResolverTypeId => "Count";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return StatescriptVariableTypeConverter.IsCompatible(expectedType, StatescriptVariableType.Int);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new CountResolverResource
		{
			Source = SourcePicker?.BuildResource(),
			SourceFolded = SourcePicker?.Folded ?? true,
			Predicate = _usePredicate ? _predicatePicker?.BuildResource() : null,
			PredicateFolded = _predicatePicker?.Folded ?? true,
		};
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_predicatePicker?.ClearCallbacks();
	}

	protected override StatescriptResolverResource? GetExistingSource(StatescriptNodeProperty? property)
	{
		return (property?.Resolver as CountResolverResource)?.Source;
	}

	protected override bool GetExistingSourceFolded(StatescriptNodeProperty? property)
	{
		return (property?.Resolver as CountResolverResource)?.SourceFolded ?? true;
	}

	protected override void BuildAdditionalRows(
		VBoxContainer root,
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged)
	{
		var existingResource = property?.Resolver as CountResolverResource;
		_usePredicate = existingResource?.Predicate is not null;

		var usePredicateCheckBox = new CheckBox
		{
			Text = "Filter by predicate",
			ButtonPressed = _usePredicate,
		};
		root.AddChild(usePredicateCheckBox);

		_predicatePicker = AddOperandPicker(
			root,
			graph,
			existingResource?.Predicate,
			"Predicate:",
			[typeof(bool)],
			existingResource?.PredicateFolded ?? true,
			onChanged,
			beginsIterationScope: true);
		_predicatePicker.Visible = _usePredicate;

		usePredicateCheckBox.Toggled += toggledOn =>
		{
			_usePredicate = toggledOn;

			if (_predicatePicker is not null)
			{
				_predicatePicker.Visible = toggledOn;
			}

			onChanged();
			RaiseLayoutSizeChanged();
		};
	}
}
#endif
