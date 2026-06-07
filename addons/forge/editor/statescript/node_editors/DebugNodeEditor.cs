// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;
using GodotCollections = Godot.Collections;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Custom editor for <c>DebugNode</c>. Lets the user choose the concrete scalar or array value type to debug, then
/// constrains the source resolver picker to that shape so debug output can be interpreted correctly.
/// </summary>
[Tool]
internal sealed partial class DebugNodeEditor : CustomNodeEditor
{
	private const string ValueTypeKey = "valueType";
	private const string IsArrayKey = "isArray";
	private const string TypeFoldKey = "_debug_type_fold";

	private StatescriptVariableType _selectedType = StatescriptVariableType.Int;
	private bool _selectedIsArray;

	[NonSerialized]
	private VBoxContainer? _inputRootContainer;

	[NonSerialized]
	private FoldableContainer? _typeFoldable;

	/// <inheritdoc/>
	public override string HandledRuntimeTypeName => "Gamesmiths.Forge.Statescript.Nodes.Action.DebugNode";

	/// <inheritdoc/>
	public override void BuildPropertySections(StatescriptNodeDiscovery.NodeTypeInfo typeInfo)
	{
		if (NodeResource.CustomData.TryGetValue(ValueTypeKey, out Variant storedType))
		{
			_selectedType = (StatescriptVariableType)storedType.AsInt32();
		}

		if (NodeResource.CustomData.TryGetValue(IsArrayKey, out Variant storedIsArray))
		{
			_selectedIsArray = storedIsArray.AsBool();
		}

		bool inputFolded = GetFoldState("_fold_input");
		FoldableContainer inputContainer = AddPropertySectionDivider(
			"Input Properties",
			InputPropertyColor,
			"_fold_input",
			inputFolded);

		_inputRootContainer = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		inputContainer.AddChild(_inputRootContainer);

		BuildTypeRow(_inputRootContainer);

		if (typeInfo.InputPropertiesInfo.Length > 0)
		{
			RebuildInputEditor(typeInfo.InputPropertiesInfo[0]);
		}
	}

	/// <inheritdoc/>
	internal override void Unbind()
	{
		base.Unbind();
		_inputRootContainer = null;
		_typeFoldable = null;
	}

	private void BuildTypeRow(VBoxContainer root)
	{
		_typeFoldable = new FoldableContainer
		{
			Title = "Type:",
			Folded = GetFoldState(TypeFoldKey, true),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		_typeFoldable.FoldingChanged += OnTypeFoldableFoldingChanged;
		root.AddChild(_typeFoldable);

		var container = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		_typeFoldable.AddChild(container);

		var headerRow = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		headerRow.AddThemeConstantOverride("separation", 5);
		container.AddChild(headerRow);

		OptionButton typeDropdown = StatescriptEditorControls.CreateVariableTypeDropdown(
			_selectedType,
			OnTypeChanged);
		typeDropdown.CustomMinimumSize = new Vector2(80, 0);
		headerRow.AddChild(typeDropdown);

		OptionButton shapeDropdown = StatescriptEditorControls.CreateValueShapeDropdown(
			_selectedIsArray,
			OnShapeChanged);
		headerRow.AddChild(shapeDropdown);

		UpdateTypeFoldableTitle();
	}

	private void OnTypeChanged(StatescriptVariableType selectedValue)
	{
		if (_selectedType == selectedValue)
		{
			return;
		}

		ChangeInputPropertyConfig(
			0,
			new GodotCollections.Dictionary { { ValueTypeKey, (int)selectedValue } },
			"Change Debug Input Type");
	}

	private void OnShapeChanged(bool isArray)
	{
		if (_selectedIsArray == isArray)
		{
			return;
		}

		ChangeInputPropertyConfig(
			0,
			new GodotCollections.Dictionary { { IsArrayKey, isArray } },
			"Change Debug Input Shape");
	}

	private void OnTypeFoldableFoldingChanged(bool folded)
	{
		SetFoldStateWithUndo(TypeFoldKey, folded);
		UpdateTypeFoldableTitle();
		RefreshInputPropertyFoldableTitles();
		ResetSize();
	}

	private void UpdateTypeFoldableTitle()
	{
		if (_typeFoldable is null)
		{
			return;
		}

		InlineConstantSummaryFormatter.ApplyFoldableTitle(
			"Type:",
			_typeFoldable,
			StatescriptEditorControls.FormatVariableTypeDisplayName(_selectedType, _selectedIsArray),
			InlineSummaryBadgeKind.Enum);
	}

	private void RebuildInputEditor(StatescriptNodeDiscovery.InputPropertyInfo originalInfo)
	{
		if (_inputRootContainer is null)
		{
			return;
		}

		ClearValueRows();
		Type clrType = StatescriptVariableTypeConverter.ToSystemType(_selectedType);
		AddInputPropertyRow(
			new StatescriptNodeDiscovery.InputPropertyInfo(originalInfo.Label, clrType, _selectedIsArray),
			0,
			_inputRootContainer);
	}

	private void ClearValueRows()
	{
		if (_inputRootContainer is null)
		{
			return;
		}

		foreach (Node child in _inputRootContainer.GetChildren())
		{
			if (child == _typeFoldable)
			{
				continue;
			}

			_inputRootContainer.RemoveChild(child);
			child.QueueFree();
		}
	}
}
#endif
