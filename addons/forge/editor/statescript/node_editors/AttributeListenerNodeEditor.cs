// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Attributes;
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
	private AttributeSelectionControl? _attributePicker;

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
		_attributePicker = null;
	}

	private void BuildSettingsSection()
	{
		FoldableContainer container = AddPropertySectionDivider(
			"Settings",
			InputPropertyColor,
			SettingsFoldKey,
			GetFoldState(SettingsFoldKey));

		var root = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		container.AddChild(root);

		_attributePicker = new AttributeSelectionControl
		{
			LabelWidth = LabelWidth,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		_attributePicker.ValueChanged += OnAttributeSelectionChanged;
		root.AddChild(_attributePicker);

		_attributePicker.SetValue(ReadStoredAttributeKey());
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
			AddScalarOutputVariableRow(root, info.Label, i, info.ValueType);
		}
	}

	private void OnAttributeSelectionChanged()
	{
		if (_attributePicker is null)
		{
			return;
		}

		SetNodeConfig(AttributeKeyConfig, _attributePicker.AttributeKey, "Change Attribute");
	}

	private string ReadStoredAttributeKey()
	{
		return NodeResource.CustomData.TryGetValue(AttributeKeyConfig, out Variant value)
			? value.AsString()
			: string.Empty;
	}
}
#endif
