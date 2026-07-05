// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class ElementAtResolverEditor : ArrayReductionResolverEditorBase
{
	private NestedResolverPicker? _indexPicker;

	public override string DisplayName => "Element At";

	public override string ResolverTypeId => "ElementAt";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(ForgeVariant128)
			|| StatescriptVariableTypeConverter.TryFromSystemType(expectedType, out _);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new ElementAtResolverResource
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
		return (property?.Resolver as ElementAtResolverResource)?.Source;
	}

	protected override bool GetExistingSourceFolded(StatescriptNodeProperty? property)
	{
		return (property?.Resolver as ElementAtResolverResource)?.SourceFolded ?? true;
	}

	protected override Type[] GetSourceExpectedTypes(Type expectedType)
	{
		return GetAllowedExpectedTypes(expectedType);
	}

	protected override void BuildAdditionalRows(
		VBoxContainer root,
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged)
	{
		var existingResource = property?.Resolver as ElementAtResolverResource;
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
