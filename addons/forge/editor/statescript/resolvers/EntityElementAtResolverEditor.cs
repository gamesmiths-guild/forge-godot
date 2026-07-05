// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class EntityElementAtResolverEditor : ArrayReductionResolverEditorBase
{
	private NestedResolverPicker? _indexPicker;

	public override string DisplayName => "Entity At";

	public override string ResolverTypeId => "EntityElementAt";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(IForgeEntity);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new EntityElementAtResolverResource
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

	protected override StatescriptResolverResource? GetExistingSource(StatescriptNodeProperty? property)
	{
		return (property?.Resolver as EntityElementAtResolverResource)?.Source;
	}

	protected override bool GetExistingSourceFolded(StatescriptNodeProperty? property)
	{
		return (property?.Resolver as EntityElementAtResolverResource)?.SourceFolded ?? true;
	}

	protected override Type[] GetSourceExpectedTypes(Type expectedType)
	{
		return [typeof(IForgeEntity)];
	}

	protected override void BuildAdditionalRows(
		VBoxContainer root,
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged)
	{
		var existingResource = property?.Resolver as EntityElementAtResolverResource;
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
