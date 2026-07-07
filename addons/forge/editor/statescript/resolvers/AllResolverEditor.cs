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
internal sealed partial class AllResolverEditor : ArrayReductionResolverEditorBase
{
	private NestedResolverPicker? _predicatePicker;

	public override string DisplayName => "All";

	public override string ResolverTypeId => "All";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(bool) || expectedType == typeof(ForgeVariant128);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new AllResolverResource
		{
			Source = SourcePicker?.BuildResource(),
			SourceFolded = SourcePicker?.Folded ?? true,
			Predicate = _predicatePicker?.BuildResource(),
			PredicateFolded = _predicatePicker?.Folded ?? true,
		};
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_predicatePicker?.ClearCallbacks();
	}

	protected override StatescriptResolverResource? GetExistingSource(StatescriptNodeProperty? property)
	{
		return (property?.Resolver as AllResolverResource)?.Source;
	}

	protected override bool GetExistingSourceFolded(StatescriptNodeProperty? property)
	{
		return (property?.Resolver as AllResolverResource)?.SourceFolded ?? true;
	}

	protected override void BuildAdditionalRows(
		VBoxContainer root,
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged)
	{
		var existingResource = property?.Resolver as AllResolverResource;
		_predicatePicker = AddOperandPicker(
			root,
			graph,
			existingResource?.Predicate,
			"Predicate:",
			[typeof(bool)],
			existingResource?.PredicateFolded ?? true,
			onChanged,
			beginsIterationScope: true);
	}
}
#endif
