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
internal sealed partial class ObjectEqualsResolverEditor : NodeEditorProperty
{
	private const float LabelWidth = 66.0f;

	private StatescriptGraph? _graph;
	private Action? _onChanged;
	private VBoxContainer? _operandsContainer;
	private NestedResolverPicker? _leftPicker;
	private NestedResolverPicker? _rightPicker;
	private string _selectedTypeId = "Entity";

	public override string DisplayName => "Object Equals";

	public override string ResolverTypeId => "ObjectEquals";

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

		var existingResource = property?.Resolver as ObjectEqualsResolverResource;
		_selectedTypeId = ResolveInitialTypeId(existingResource?.ObjectTypeId);

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		root.AddChild(CreateTypeRow());

		_operandsContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		root.AddChild(_operandsContainer);

		BuildOperandPickers(existingResource);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new ObjectEqualsResolverResource
		{
			ObjectTypeId = _selectedTypeId,
			Left = _leftPicker?.BuildResource(),
			LeftFolded = _leftPicker?.Folded ?? true,
			Right = _rightPicker?.BuildResource(),
			RightFolded = _rightPicker?.Folded ?? true,
		};
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Object Equals";
		return true;
	}

	public override bool TryGetHighlightedVariableName(out string variableName)
	{
		if (_leftPicker is not null && _leftPicker.TryGetHighlightedVariableName(out variableName))
		{
			return true;
		}

		if (_rightPicker is not null && _rightPicker.TryGetHighlightedVariableName(out variableName))
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
		_leftPicker?.ClearCallbacks();
		_rightPicker?.ClearCallbacks();
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
		BuildOperandPickers(null);
		_onChanged?.Invoke();
		RaiseLayoutSizeChanged();
	}

	private void BuildOperandPickers(ObjectEqualsResolverResource? existingResource)
	{
		if (_graph is null || _operandsContainer is null)
		{
			return;
		}

		NestedResolverEditorUtilities.ClearContainer(_operandsContainer);

		Type clrType = StatescriptObjectVariableTypeRegistry.TryGet(
			_selectedTypeId,
			out StatescriptObjectVariableType? descriptor)
				? descriptor.ClrType
				: typeof(IForgeEntity);

		_leftPicker = new NestedResolverPicker();
		_leftPicker.Initialize(
			_graph,
			existingResource?.Left,
			"Left:",
			[clrType],
			isArray: false,
			existingResource?.LeftFolded ?? true,
			() => _onChanged?.Invoke(),
			RaiseLayoutSizeChanged,
			IterationScope);
		_operandsContainer.AddChild(_leftPicker);

		_rightPicker = new NestedResolverPicker();
		_rightPicker.Initialize(
			_graph,
			existingResource?.Right,
			"Right:",
			[clrType],
			isArray: false,
			existingResource?.RightFolded ?? true,
			() => _onChanged?.Invoke(),
			RaiseLayoutSizeChanged,
			IterationScope);
		_operandsContainer.AddChild(_rightPicker);
	}
}
#endif
