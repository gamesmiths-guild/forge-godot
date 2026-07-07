// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class AppendResolverEditor : ArrayTransformResolverEditorBase
{
	private NestedResolverPicker? _elementPicker;

	public override string DisplayName => "Append";

	public override string ResolverTypeId => "Append";

	public override void SaveTo(StatescriptNodeProperty property)
	{
		var resource = new AppendResolverResource
		{
			Source = SourcePicker?.BuildResource(),
			SourceFolded = SourcePicker?.Folded ?? true,
			ElementFolded = _elementPicker?.Folded ?? true,
		};

		if (_elementPicker?.BuildResource() is StatescriptResolverResource element)
		{
			resource.Elements.Add(element);
		}

		property.Resolver = resource;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_elementPicker?.ClearCallbacks();
	}

	protected override void BuildAdditionalRows(
		VBoxContainer root,
		StatescriptGraph graph,
		Type expectedType,
		Action onChanged)
	{
		var existingResource = ExistingResource as AppendResolverResource;
		StatescriptResolverResource? existingElement = existingResource?.Elements.Count > 0
			? existingResource.Elements[0]
			: null;

		_elementPicker = AddOperandPicker(
			root,
			graph,
			existingElement,
			"Element:",
			GetAllowedExpectedTypes(expectedType),
			existingResource?.ElementFolded ?? true,
			onChanged);
	}
}
#endif
