// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class ConcatResolverEditor : ArrayTransformResolverEditorBase
{
	private NestedResolverPicker? _secondPicker;

	public override string DisplayName => "Concat";

	public override string ResolverTypeId => "Concat";

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new ConcatResolverResource
		{
			Source = SourcePicker?.BuildResource(),
			SourceFolded = SourcePicker?.Folded ?? true,
			Second = _secondPicker?.BuildResource(),
			SecondFolded = _secondPicker?.Folded ?? true,
		};
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_secondPicker?.ClearCallbacks();
	}

	public override bool TryGetHighlightedVariableName(out string variableName)
	{
		if (base.TryGetHighlightedVariableName(out variableName))
		{
			return true;
		}

		return _secondPicker is not null && _secondPicker.TryGetHighlightedVariableName(out variableName);
	}

	protected override void BuildAdditionalRows(
		VBoxContainer root,
		StatescriptGraph graph,
		Type expectedType,
		Action onChanged)
	{
		var existingResource = ExistingResource as ConcatResolverResource;
		_secondPicker = AddOperandPicker(
			root,
			graph,
			existingResource?.Second,
			"Second:",
			GetAllowedExpectedTypes(expectedType),
			existingResource?.SecondFolded ?? true,
			onChanged,
			isArray: true);
	}
}
#endif
