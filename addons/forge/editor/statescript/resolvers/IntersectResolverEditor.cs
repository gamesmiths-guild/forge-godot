// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class IntersectResolverEditor : ArrayTransformResolverEditorBase
{
	private NestedResolverPicker? _otherPicker;

	public override string DisplayName => "Intersect";

	public override string ResolverTypeId => "Intersect";

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new IntersectResolverResource
		{
			Source = SourcePicker?.BuildResource(),
			SourceFolded = SourcePicker?.Folded ?? true,
			Other = _otherPicker?.BuildResource(),
			OtherFolded = _otherPicker?.Folded ?? true,
		};
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_otherPicker?.ClearCallbacks();
	}

	public override bool TryGetHighlightedVariableName(out string variableName)
	{
		if (base.TryGetHighlightedVariableName(out variableName))
		{
			return true;
		}

		return _otherPicker is not null && _otherPicker.TryGetHighlightedVariableName(out variableName);
	}

	protected override void BuildAdditionalRows(
		VBoxContainer root,
		StatescriptGraph graph,
		Type expectedType,
		Action onChanged)
	{
		var existingResource = ExistingResource as IntersectResolverResource;
		_otherPicker = AddOperandPicker(
			root,
			graph,
			existingResource?.Other,
			"Keep:",
			GetAllowedExpectedTypes(expectedType),
			existingResource?.OtherFolded ?? true,
			onChanged,
			isArray: true);
	}
}
#endif
