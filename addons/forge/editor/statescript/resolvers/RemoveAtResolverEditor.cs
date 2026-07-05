// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class RemoveAtResolverEditor : ArrayTransformResolverEditorBase
{
	private NestedResolverPicker? _indexPicker;

	public override string DisplayName => "Remove At";

	public override string ResolverTypeId => "RemoveAt";

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new RemoveAtResolverResource
		{
			Source = SourcePicker?.BuildResource(),
			SourceFolded = SourcePicker?.Folded ?? true,
			Index = _indexPicker?.BuildResource(),
			IndexFolded = _indexPicker?.Folded ?? true,
		};
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_indexPicker?.ClearCallbacks();
	}

	protected override void BuildAdditionalRows(
		VBoxContainer root,
		StatescriptGraph graph,
		Type expectedType,
		Action onChanged)
	{
		var existingResource = ExistingResource as RemoveAtResolverResource;
		_indexPicker = AddOperandPicker(
			root,
			graph,
			existingResource?.Index,
			"Index:",
			[typeof(int)],
			existingResource?.IndexFolded ?? true,
			onChanged);
	}
}
#endif
