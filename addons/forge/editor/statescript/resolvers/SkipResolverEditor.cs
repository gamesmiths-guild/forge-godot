// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class SkipResolverEditor : ArrayTransformResolverEditorBase
{
	private NestedResolverPicker? _countPicker;

	public override string DisplayName => "Skip";

	public override string ResolverTypeId => "Skip";

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new SkipResolverResource
		{
			Source = SourcePicker?.BuildResource(),
			SourceFolded = SourcePicker?.Folded ?? true,
			Count = _countPicker?.BuildResource(),
			CountFolded = _countPicker?.Folded ?? true,
		};
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_countPicker?.ClearCallbacks();
	}

	protected override void BuildAdditionalRows(
		VBoxContainer root,
		StatescriptGraph graph,
		Type expectedType,
		Action onChanged)
	{
		var existingResource = ExistingResource as SkipResolverResource;
		_countPicker = AddOperandPicker(
			root,
			graph,
			existingResource?.Count,
			"Count:",
			[typeof(int)],
			existingResource?.CountFolded ?? true,
			onChanged);
	}
}
#endif
