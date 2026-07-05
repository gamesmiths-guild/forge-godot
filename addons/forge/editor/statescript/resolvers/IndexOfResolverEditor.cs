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
internal sealed partial class IndexOfResolverEditor : ArrayReductionResolverEditorBase
{
	private NestedResolverPicker? _valuePicker;

	public override string DisplayName => "Index Of";

	public override string ResolverTypeId => "IndexOf";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return StatescriptVariableTypeConverter.IsCompatible(expectedType, StatescriptVariableType.Int);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new IndexOfResolverResource
		{
			Source = SourcePicker?.BuildResource(),
			SourceFolded = SourcePicker?.Folded ?? true,
			Value = _valuePicker?.BuildResource(),
			ValueFolded = _valuePicker?.Folded ?? true,
		};
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_valuePicker?.ClearCallbacks();
	}

	protected override StatescriptResolverResource? GetExistingSource(StatescriptNodeProperty? property)
	{
		return (property?.Resolver as IndexOfResolverResource)?.Source;
	}

	protected override bool GetExistingSourceFolded(StatescriptNodeProperty? property)
	{
		return (property?.Resolver as IndexOfResolverResource)?.SourceFolded ?? true;
	}

	protected override Type[] GetSourceExpectedTypes(Type expectedType)
	{
		return [typeof(ForgeVariant128)];
	}

	protected override void BuildAdditionalRows(
		VBoxContainer root,
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged)
	{
		var existingResource = property?.Resolver as IndexOfResolverResource;
		_valuePicker = AddOperandPicker(
			root,
			graph,
			existingResource?.Value,
			"Value:",
			[typeof(ForgeVariant128)],
			existingResource?.ValueFolded ?? true,
			onChanged);
	}
}
#endif
