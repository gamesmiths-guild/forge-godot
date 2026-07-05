// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class WhereResolverEditor : ArrayTransformResolverEditorBase
{
	private NestedResolverPicker? _predicatePicker;

	public override string DisplayName => "Where (Filter)";

	public override string ResolverTypeId => "Where";

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new WhereResolverResource
		{
			Source = SourcePicker?.BuildResource(),
			SourceFolded = SourcePicker?.Folded ?? true,
			Predicate = _predicatePicker?.BuildResource(),
			PredicateFolded = _predicatePicker?.Folded ?? true,
		};
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_predicatePicker?.ClearCallbacks();
	}

	public override bool TryGetHighlightedVariableName(out string variableName)
	{
		if (base.TryGetHighlightedVariableName(out variableName))
		{
			return true;
		}

		return _predicatePicker is not null && _predicatePicker.TryGetHighlightedVariableName(out variableName);
	}

	protected override void BuildAdditionalRows(
		VBoxContainer root,
		StatescriptGraph graph,
		Type expectedType,
		Action onChanged)
	{
		var existingResource = ExistingResource as WhereResolverResource;
		_predicatePicker = AddOperandPicker(
			root,
			graph,
			existingResource?.Predicate,
			"Predicate:",
			[typeof(bool)],
			existingResource?.PredicateFolded ?? true,
			onChanged,
			beginsIterationScope: true);
	}
}
#endif
