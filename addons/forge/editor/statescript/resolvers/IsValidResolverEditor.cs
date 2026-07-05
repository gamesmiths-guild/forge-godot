// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class IsValidResolverEditor : NodeEditorProperty
{
	private const float LabelWidth = 66.0f;

	private StatescriptGraph? _graph;
	private Action? _onChanged;
	private VBoxContainer? _sourceContainer;
	private NestedResolverPicker? _sourcePicker;
	private string _selectedTypeId = "Entity";

	public override string DisplayName => "Is Valid";

	public override string ResolverTypeId => "IsValid";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(bool) || expectedType == typeof(Variant128);
	}

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		_graph = graph;
		_onChanged = onChanged;

		var existingResource = property?.Resolver as IsValidResolverResource;
		_selectedTypeId = ResolveInitialTypeId(existingResource?.ObjectTypeId);

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		root.AddChild(CreateTypeRow());

		_sourceContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		root.AddChild(_sourceContainer);

		BuildSourcePicker(existingResource);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new IsValidResolverResource
		{
			ObjectTypeId = _selectedTypeId,
			Source = _sourcePicker?.BuildResource(),
			SourceFolded = _sourcePicker?.Folded ?? true,
		};
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Is Valid";
		return true;
	}

	public override bool TryGetHighlightedVariableName(out string variableName)
	{
		if (_sourcePicker is not null && _sourcePicker.TryGetHighlightedVariableName(out variableName))
		{
			return true;
		}

		variableName = string.Empty;
		return false;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_sourcePicker?.ClearCallbacks();
	}

	private static string ResolveInitialTypeId(string? storedTypeId)
	{
		return !string.IsNullOrEmpty(storedTypeId)
			&& StatescriptObjectVariableTypeRegistry.TryGet(storedTypeId, out _)
				? storedTypeId
				: "Entity";
	}

	private HBoxContainer CreateTypeRow()
	{
		var dropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		int selectedIndex = 0;

		for (int i = 0; i < StatescriptObjectVariableTypeRegistry.All.Count; i++)
		{
			StatescriptObjectVariableType descriptor = StatescriptObjectVariableTypeRegistry.All[i];
			dropdown.AddItem(descriptor.DisplayName);

			if (descriptor.TypeId == _selectedTypeId)
			{
				selectedIndex = i;
			}
		}

		dropdown.Selected = selectedIndex;
		dropdown.ItemSelected += index => OnTypeChanged(StatescriptObjectVariableTypeRegistry.All[(int)index].TypeId);
		return ResolverEditorLayoutUtilities.CreateLabeledRow("Type:", dropdown, LabelWidth);
	}

	private void OnTypeChanged(string typeId)
	{
		if (_selectedTypeId == typeId)
		{
			return;
		}

		_selectedTypeId = typeId;
		BuildSourcePicker(null);
		_onChanged?.Invoke();
		RaiseLayoutSizeChanged();
	}

	private void BuildSourcePicker(IsValidResolverResource? existingResource)
	{
		if (_graph is null || _sourceContainer is null)
		{
			return;
		}

		NestedResolverEditorUtilities.ClearContainer(_sourceContainer);

		Type clrType = StatescriptObjectVariableTypeRegistry.TryGet(
			_selectedTypeId,
			out StatescriptObjectVariableType? descriptor)
				? descriptor.ClrType
				: typeof(IForgeEntity);

		_sourcePicker = new NestedResolverPicker();
		_sourcePicker.Initialize(
			_graph,
			existingResource?.Source,
			"Source:",
			[clrType],
			isArray: false,
			existingResource?.SourceFolded ?? true,
			() => _onChanged?.Invoke(),
			RaiseLayoutSizeChanged,
			IterationScope);
		_sourceContainer.AddChild(_sourcePicker);
	}
}
#endif
