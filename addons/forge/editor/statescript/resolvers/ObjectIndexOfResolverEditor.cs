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
internal sealed partial class ObjectIndexOfResolverEditor : ArrayReductionResolverEditorBase
{
	private EntityOperandPicker? _valuePicker;

	public override string DisplayName => "Entity Index Of";

	public override string ResolverTypeId => "ObjectIndexOf";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return StatescriptVariableTypeConverter.IsCompatible(expectedType, StatescriptVariableType.Int);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new ObjectIndexOfResolverResource
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
		return (property?.Resolver as ObjectIndexOfResolverResource)?.Source;
	}

	protected override bool GetExistingSourceFolded(StatescriptNodeProperty? property)
	{
		return (property?.Resolver as ObjectIndexOfResolverResource)?.SourceFolded ?? true;
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
		var existingResource = property?.Resolver as ObjectIndexOfResolverResource;
		_valuePicker = new EntityOperandPicker();
		_valuePicker.Initialize(
			graph,
			existingResource?.Value,
			"Entity:",
			66.0f,
			onChanged,
			RaiseLayoutSizeChanged,
			IterationScope);
		root.AddChild(_valuePicker);
	}
}
#endif
