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

	private StatescriptVariableType _selectedType = StatescriptVariableType.Int;
	private VBoxContainer? _inputEditorContainer;
	private StatescriptNodeDiscovery.NodeTypeInfo? _cachedTypeInfo;
	private OptionButton? _typeDropdown;

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

		var inputFolded = GetFoldState("_fold_input");
		FoldableContainer inputContainer = AddPropertySectionDivider(
			"Input Properties",
			InputPropertyColor,
			"_fold_input",
			inputFolded);

		var root = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		inputContainer.AddChild(root);

		BuildTypeRow(root);

		_inputEditorContainer = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		root.AddChild(_inputEditorContainer);

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
		var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		root.AddChild(row);

		var label = new Label
		{
			Text = "Type",
			CustomMinimumSize = new Vector2(60, 0),
		};
		label.AddThemeColorOverride("font_color", InputPropertyColor);
		row.AddChild(label);

		_typeDropdown = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
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
		row.AddChild(_typeDropdown);
	}

	private int FindSelectedTypeIndex(OptionButton dropdown)
	{
		for (var i = 0; i < dropdown.ItemCount; i++)
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

	private void RebuildInputEditor(StatescriptNodeDiscovery.InputPropertyInfo originalInfo)
	{
		if (_inputEditorContainer is null)
		{
			return;
		}

		ClearContainer(_inputEditorContainer);
		Type clrType = StatescriptVariableTypeConverter.ToSystemType(_selectedType);
		AddInputPropertyRow(
			new StatescriptNodeDiscovery.InputPropertyInfo(originalInfo.Label, clrType),
			0,
			_inputEditorContainer);
	}
}
#endif
