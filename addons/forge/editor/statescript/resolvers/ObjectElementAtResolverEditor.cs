// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class ObjectElementAtResolverEditor : ArrayReductionResolverEditorBase
{
	private NestedResolverPicker? _indexPicker;

	public override string DisplayName => "Element At";

	public override string ResolverTypeId => "ObjectElementAt";

	public override bool IsCompatibleWith(Type expectedType)
	{
		// Entity arrays use the dedicated Entity At resolver (an entity resolver usable in attribute/tag reads).
		return expectedType != typeof(IForgeEntity)
			&& StatescriptObjectVariableTypeRegistry.IsObjectType(expectedType);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new ObjectElementAtResolverResource
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
		return (property?.Resolver as ObjectElementAtResolverResource)?.Source;
	}

	protected override bool GetExistingSourceFolded(StatescriptNodeProperty? property)
	{
		return (property?.Resolver as ObjectElementAtResolverResource)?.SourceFolded ?? true;
	}

	protected override Type[] GetSourceExpectedTypes(Type expectedType)
	{
		return [expectedType];
	}

	protected override void BuildAdditionalRows(
		VBoxContainer root,
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged)
	{
		var existingResource = property?.Resolver as ObjectElementAtResolverResource;
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
