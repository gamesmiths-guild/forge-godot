// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class ObjectContainsResolverEditor : ArrayReductionResolverEditorBase
{
	private EntityOperandPicker? _valuePicker;

	public override string DisplayName => "Contains Entity";

	public override string ResolverTypeId => "ObjectContains";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(bool) || expectedType == typeof(ForgeVariant128);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new ObjectContainsResolverResource
		{
			Source = SourcePicker?.BuildResource(),
			SourceFolded = SourcePicker?.Folded ?? true,
			Value = _valuePicker?.BuildResource(),
		};
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_valuePicker?.ClearCallbacks();
	}

	protected override StatescriptResolverResource? GetExistingSource(StatescriptNodeProperty? property)
	{
		return (property?.Resolver as ObjectContainsResolverResource)?.Source;
	}

	protected override bool GetExistingSourceFolded(StatescriptNodeProperty? property)
	{
		return (property?.Resolver as ObjectContainsResolverResource)?.SourceFolded ?? true;
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
		var existingResource = property?.Resolver as ObjectContainsResolverResource;
		_valuePicker = new EntityOperandPicker();
		_valuePicker.Initialize(
			graph,
			existingResource?.Value,
			"Entity:",
			onChanged,
			RaiseLayoutSizeChanged,
			IterationScope);
		root.AddChild(_valuePicker);
	}
}
#endif
