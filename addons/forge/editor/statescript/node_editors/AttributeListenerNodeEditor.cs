// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Node editor for the <c>AttributeListenerNode</c>. The observed attribute is a constructor argument (a
/// <see cref="Forge.Core.StringKey"/>), not an input property, so this editor renders a Settings section with an
/// attribute picker (set + attribute dropdowns) that persists the fully qualified key into the node's
/// <c>CustomData</c> under the <c>attributeKey</c> constructor-parameter name.
/// </summary>
[Tool]
internal sealed partial class AttributeListenerNodeEditor : CustomNodeEditor
{
	private const string SettingsFoldKey = "_fold_settings";
	private const string InputFoldKey = "_fold_input";
	private const string OutputFoldKey = "_fold_output";

	// Matches the AttributeListenerNode constructor parameter name consumed by StatescriptGraphBuilder.
	private const string AttributeKeyConfig = "attributeKey";

	private const float LabelWidth = 60.0f;

	[NonSerialized]
	private OptionButton? _setDropdown;

	[NonSerialized]
	private OptionButton? _attributeDropdown;

	private string _selectedSet = string.Empty;
	private string _selectedAttribute = string.Empty;

	/// <inheritdoc/>
	public override string HandledRuntimeTypeName =>
		"Gamesmiths.Forge.Statescript.Nodes.State.AttributeListenerNode";

	/// <inheritdoc/>
	public override void BuildPropertySections(StatescriptNodeDiscovery.NodeTypeInfo typeInfo)
	{
		BuildSettingsSection();
		BuildInputSection(typeInfo);
		BuildOutputSection(typeInfo);
	}

	/// <inheritdoc/>
	internal override void Unbind()
	{
		base.Unbind();
		_setDropdown = null;
		_attributeDropdown = null;
	}

	private void BuildSettingsSection()
	{
		ParseAttributeKey(ReadStoredAttributeKey());

		FoldableContainer container = AddPropertySectionDivider(
			"Settings",
			InputPropertyColor,
			SettingsFoldKey,
			GetFoldState(SettingsFoldKey));

		var root = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		container.AddChild(root);

		_setDropdown = new SearchableOptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		_setDropdown.ItemSelected += OnSetChanged;
		root.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow("Set:", _setDropdown, LabelWidth));

		_attributeDropdown = new SearchableOptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		_attributeDropdown.ItemSelected += OnAttributeChanged;
		root.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow("Attr:", _attributeDropdown, LabelWidth));

		PopulateSetDropdown();
		PopulateAttributeDropdown();

		// Keep the stored key in sync with the resolved selection so a freshly created node (or one whose attribute was
		// renamed/removed) persists a valid key without requiring the user to touch the dropdowns first.
		SyncAttributeKeyToSelection();
	}

	private void BuildInputSection(StatescriptNodeDiscovery.NodeTypeInfo typeInfo)
	{
		if (typeInfo.InputPropertiesInfo.Length == 0)
		{
			return;
		}

		FoldableContainer container = AddPropertySectionDivider(
			"Input Properties",
			InputPropertyColor,
			InputFoldKey,
			GetFoldState(InputFoldKey));

		var root = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		container.AddChild(root);

		for (int i = 0; i < typeInfo.InputPropertiesInfo.Length; i++)
		{
			AddInputPropertyRow(typeInfo.InputPropertiesInfo[i], i, root);
		}
	}

	private void BuildOutputSection(StatescriptNodeDiscovery.NodeTypeInfo typeInfo)
	{
		if (typeInfo.OutputVariablesInfo.Length == 0)
		{
			return;
		}

		FoldableContainer container = AddPropertySectionDivider(
			"Output Variables",
			OutputVariableColor,
			OutputFoldKey,
			GetFoldState(OutputFoldKey));

		var root = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		container.AddChild(root);

		for (int i = 0; i < typeInfo.OutputVariablesInfo.Length; i++)
		{
			StatescriptNodeDiscovery.OutputVariableInfo info = typeInfo.OutputVariablesInfo[i];
			AddScalarOutputRow(root, info.Label, i, info.ValueType);
		}
	}

	private void AddScalarOutputRow(VBoxContainer container, string label, int index, Type valueType)
	{
		List<string> candidates = GetScalarCandidateVariableNames(valueType);

		string? current =
			FindBinding(StatescriptPropertyDirection.Output, index)?.Resolver is VariableResolverResource resolver
				? resolver.VariableName
				: null;

		// Drop a stale binding whose variable no longer matches the output's type so it is not silently kept.
		if (!string.IsNullOrEmpty(current) && !candidates.Contains(current))
		{
			current = null;
			RemoveBinding(StatescriptPropertyDirection.Output, index);
		}

		AddOutputVariableBadgeRow(
			container,
			label,
			$"_fold_output_{index}",
			candidates,
			current,
			variableName => OnScalarOutputSelected(variableName, index, valueType));
	}

	private void OnScalarOutputSelected(string? variableName, int index, Type valueType)
	{
		if (string.IsNullOrEmpty(variableName))
		{
			RemoveBinding(StatescriptPropertyDirection.Output, index);
		}
		else
		{
			StatescriptVariableTypeConverter.TryFromSystemType(valueType, out StatescriptVariableType variableType);

			EnsureBinding(StatescriptPropertyDirection.Output, index).Resolver = new VariableResolverResource
			{
				VariableName = variableName,
				Scope = VariableScope.Graph,
				ObjectTypeId = string.Empty,
				VariableType = variableType,
				IsArray = false,
			};
		}

		RaisePropertyBindingChanged();
		ResetSize();
	}

	private List<string> GetScalarCandidateVariableNames(Type valueType)
	{
		var names = new List<string>();

		if (!StatescriptVariableTypeConverter.TryFromSystemType(valueType, out StatescriptVariableType targetType))
		{
			return names;
		}

		foreach (StatescriptGraphVariable variable in Graph.Variables)
		{
			if (variable.IsArray
				|| string.IsNullOrEmpty(variable.VariableName)
				|| !string.IsNullOrEmpty(variable.ObjectTypeId))
			{
				continue;
			}

			if (variable.VariableType == targetType)
			{
				names.Add(variable.VariableName);
			}
		}

		return names;
	}

	private void OnSetChanged(long index)
	{
		if (_setDropdown is null)
		{
			return;
		}

		_selectedSet = _setDropdown.GetItemText(_setDropdown.Selected);
		_selectedAttribute = string.Empty;
		PopulateAttributeDropdown();
		SaveAttributeKey();
	}

	private void OnAttributeChanged(long index)
	{
		if (_attributeDropdown is null)
		{
			return;
		}

		_selectedAttribute = _attributeDropdown.GetItemText(_attributeDropdown.Selected);
		SaveAttributeKey();
	}

	private void PopulateSetDropdown()
	{
		if (_setDropdown is null)
		{
			return;
		}

		_setDropdown.Clear();
		foreach (string option in EditorUtils.GetAttributeSetOptions())
		{
			_setDropdown.AddItem(option);
		}

		for (int i = 0; i < _setDropdown.ItemCount; i++)
		{
			if (_setDropdown.GetItemText(i) == _selectedSet)
			{
				_setDropdown.Selected = i;
				return;
			}
		}

		if (_setDropdown.ItemCount > 0)
		{
			_setDropdown.Selected = 0;
			_selectedSet = _setDropdown.GetItemText(0);
		}
		else
		{
			_selectedSet = string.Empty;
		}
	}

	private void PopulateAttributeDropdown()
	{
		if (_attributeDropdown is null)
		{
			return;
		}

		_attributeDropdown.Clear();
		foreach (string option in EditorUtils.GetAttributeOptions(_selectedSet))
		{
			_attributeDropdown.AddItem(option);
		}

		for (int i = 0; i < _attributeDropdown.ItemCount; i++)
		{
			if (string.Equals(
				_attributeDropdown.GetItemText(i),
				_selectedAttribute,
				StringComparison.OrdinalIgnoreCase))
			{
				_attributeDropdown.Selected = i;
				_selectedAttribute = _attributeDropdown.GetItemText(i);
				return;
			}
		}

		if (_attributeDropdown.ItemCount > 0)
		{
			_attributeDropdown.Selected = 0;
			_selectedAttribute = _attributeDropdown.GetItemText(0);
		}
		else
		{
			_selectedAttribute = string.Empty;
		}
	}

	private void SaveAttributeKey()
	{
		SetNodeConfig(AttributeKeyConfig, BuildCombinedKey(), "Change Attribute");
	}

	private void SyncAttributeKeyToSelection()
	{
		string combined = BuildCombinedKey();

		if (combined.Length == 0 || combined == ReadStoredAttributeKey())
		{
			return;
		}

		NodeResource.CustomData[AttributeKeyConfig] = combined;
		NotifyGraphResourceChanged();
	}

	private string BuildCombinedKey()
	{
		return string.IsNullOrEmpty(_selectedSet) || string.IsNullOrEmpty(_selectedAttribute)
			? string.Empty
			: $"{_selectedSet}.{_selectedAttribute}";
	}

	private string ReadStoredAttributeKey()
	{
		return NodeResource.CustomData.TryGetValue(AttributeKeyConfig, out Variant value)
			? value.AsString()
			: string.Empty;
	}

	private void ParseAttributeKey(string key)
	{
		if (string.IsNullOrEmpty(key))
		{
			return;
		}

		// Attribute set class names carry no dots, so the first dot separates the set from the attribute name.
		int dot = key.IndexOf('.', StringComparison.Ordinal);

		if (dot <= 0 || dot >= key.Length - 1)
		{
			_selectedSet = key;
			_selectedAttribute = string.Empty;
			return;
		}

		_selectedSet = key[..dot];
		_selectedAttribute = key[(dot + 1)..];
	}
}
#endif
