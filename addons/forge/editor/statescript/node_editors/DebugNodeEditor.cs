// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Custom editor for <c>DebugNode</c>. Lets the user choose the concrete value type to debug, then constrains the
/// source resolver picker to that type so debug output can be interpreted correctly.
/// </summary>
[Tool]
internal sealed partial class DebugNodeEditor : CustomNodeEditor
{
	private const string ValueTypeKey = "valueType";
	private const string TypeFoldKey = "_debug_type_fold";

	private StatescriptVariableType _selectedType = StatescriptVariableType.Int;
	private VBoxContainer? _inputRootContainer;
	private VBoxContainer? _inputEditorContainer;
	private StatescriptNodeDiscovery.NodeTypeInfo? _cachedTypeInfo;
	private OptionButton? _typeDropdown;
	private FoldableContainer? _typeFoldable;

	/// <inheritdoc/>
	public override string HandledRuntimeTypeName => "Gamesmiths.Forge.Statescript.Nodes.Action.DebugNode";

	/// <inheritdoc/>
	public override void BuildPropertySections(StatescriptNodeDiscovery.NodeTypeInfo typeInfo)
	{
		_cachedTypeInfo = typeInfo;

		if (NodeResource.CustomData.TryGetValue(ValueTypeKey, out Variant storedType))
		{
			_selectedType = (StatescriptVariableType)storedType.AsInt32();
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

	private static void AddTypeOption(OptionButton dropdown, StatescriptVariableType type, string label)
	{
		dropdown.AddItem(label, (int)type);
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
		container.AddChild(headerRow);

		_typeDropdown = new OptionButton
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(80, 0),
		};
		AddTypeOption(_typeDropdown, StatescriptVariableType.Bool, "Bool");
		AddTypeOption(_typeDropdown, StatescriptVariableType.Int, "Int");
		AddTypeOption(_typeDropdown, StatescriptVariableType.Float, "Float");
		AddTypeOption(_typeDropdown, StatescriptVariableType.Double, "Double");
		AddTypeOption(_typeDropdown, StatescriptVariableType.Vector2, "Vector2");
		AddTypeOption(_typeDropdown, StatescriptVariableType.Vector3, "Vector3");
		AddTypeOption(_typeDropdown, StatescriptVariableType.Vector4, "Vector4");
		AddTypeOption(_typeDropdown, StatescriptVariableType.Plane, "Plane");
		AddTypeOption(_typeDropdown, StatescriptVariableType.Quaternion, "Quaternion");
		_typeDropdown.Selected = FindSelectedTypeIndex(_typeDropdown);
		_typeDropdown.ItemSelected += OnTypeDropdownItemSelected;
		headerRow.AddChild(_typeDropdown);
		UpdateTypeFoldableTitle();
	}

	private int FindSelectedTypeIndex(OptionButton dropdown)
	{
		for (int i = 0; i < dropdown.ItemCount; i++)
		{
			if (dropdown.GetItemId(i) == (int)_selectedType)
			{
				return i;
			}
		}

		return 0;
	}

	private void OnTypeDropdownItemSelected(long index)
	{
		if (_typeDropdown is null)
		{
			return;
		}

		var selectedValue = (StatescriptVariableType)_typeDropdown.GetItemId((int)index);
		NodeResource.CustomData[ValueTypeKey] = Variant.From((int)selectedValue);
		_selectedType = selectedValue;
		UpdateTypeFoldableTitle();

		RemoveBinding(StatescriptPropertyDirection.Input, 0);
		ActiveResolverEditors.Remove(new PropertySlotKey(StatescriptPropertyDirection.Input, 0));

		if (_cachedTypeInfo is not null
			&& _inputEditorContainer is not null
			&& _cachedTypeInfo.InputPropertiesInfo.Length > 0)
		{
			RebuildInputEditor(_cachedTypeInfo.InputPropertiesInfo[0]);
		}

		RaisePropertyBindingChanged();
		ResetSize();
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
			StatescriptVariableTypeConverter.GetDisplayName(_selectedType),
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
		_inputEditorContainer = _inputRootContainer;
		AddInputPropertyRow(
			new StatescriptNodeDiscovery.InputPropertyInfo(originalInfo.Label, clrType),
			0,
			_inputEditorContainer);
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
			child.Free();
		}
	}
}
#endif
