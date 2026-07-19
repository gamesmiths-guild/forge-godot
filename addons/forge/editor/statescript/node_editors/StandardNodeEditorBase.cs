// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Reusable custom node editor that renders a node's constructor parameters (behavior-selecting enums and flags),
/// standard input-property rows, and output-variable rows with correct object-lane binding.
/// </summary>
/// <remarks>
/// <para>Subclasses declare their handled node type, any constructor parameters, and which output variables are
/// object-backed (so those bind through their object variable type instead of the default value-lane path, which
/// would otherwise break the panel).</para>
/// </remarks>
internal abstract partial class StandardNodeEditorBase : CustomNodeEditor
{
	private const string SettingsFoldKey = "_fold_settings";
	private const string InputFoldKey = "_fold_input";
	private const string OutputFoldKey = "_fold_output";

	/// <summary>
	/// Gets the constructor parameters exposed as editor controls. Empty by default.
	/// </summary>
	protected virtual IReadOnlyList<NodeConfigParam> ConstructorParams => [];

	/// <inheritdoc/>
	public override void BuildPropertySections(StatescriptNodeDiscovery.NodeTypeInfo typeInfo)
	{
		BuildSettingsSection();
		BuildInputSection(typeInfo);
		BuildOutputSection(typeInfo);
	}

	/// <summary>
	/// Gets the object variable type id for an object-backed output variable, or <see langword="null"/> when the
	/// output is value-lane and should use the default rendering.
	/// </summary>
	/// <param name="outputIndex">The output variable index.</param>
	/// <returns>The object type id, or <see langword="null"/> for value-lane outputs.</returns>
	protected virtual string? GetOutputObjectTypeId(int outputIndex)
	{
		return null;
	}

	private void BuildSettingsSection()
	{
		IReadOnlyList<NodeConfigParam> parameters = ConstructorParams;

		if (parameters.Count == 0)
		{
			return;
		}

		FoldableContainer container = AddPropertySectionDivider(
			"Settings",
			InputPropertyColor,
			SettingsFoldKey,
			GetFoldState(SettingsFoldKey));

		var root = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		container.AddChild(root);

		foreach (NodeConfigParam parameter in parameters)
		{
			root.AddChild(BuildParamRow(parameter));
		}
	}

	private Control BuildParamRow(NodeConfigParam parameter)
	{
		if (parameter.EnumNames is { Length: > 0 } enumNames)
		{
			string currentName = ReadStringConfig(parameter.Key, parameter.DefaultName ?? enumNames[0]);
			int currentIndex = System.Array.IndexOf(enumNames, currentName);

			var dropdown = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
			for (int i = 0; i < enumNames.Length; i++)
			{
				dropdown.AddItem(enumNames[i]);
			}

			dropdown.Selected = currentIndex >= 0 ? currentIndex : 0;

			// Enum members are stored by name so flags enums parse correctly at graph-build time.
			dropdown.ItemSelected += index =>
				SetNodeConfig(parameter.Key, enumNames[(int)index], $"Change {parameter.Label}");

			return ResolverEditorLayoutUtilities.CreateLabeledRow($"{parameter.Label}:", dropdown, 96.0f);
		}

		var checkBox = new CheckBox
		{
			Text = parameter.Label,
			ButtonPressed = ReadBoolConfig(parameter.Key, parameter.DefaultBool),
		};
		checkBox.Toggled += pressed => SetNodeConfig(parameter.Key, pressed, $"Change {parameter.Label}");
		return checkBox;
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
			string? objectTypeId = GetOutputObjectTypeId(i);

			if (objectTypeId is null)
			{
				StatescriptNodeDiscovery.OutputVariableInfo info = typeInfo.OutputVariablesInfo[i];
				AddScalarOutputVariableRow(root, info.Label, i, info.ValueType);
				continue;
			}

			AddObjectOutputRow(root, typeInfo.OutputVariablesInfo[i].Label, i, objectTypeId);
		}
	}

	private void AddObjectOutputRow(VBoxContainer container, string label, int index, string objectTypeId)
	{
		var candidates = new List<string>();

		foreach (StatescriptGraphVariable variable in Graph.Variables)
		{
			if (variable.ObjectTypeId == objectTypeId
				&& !variable.IsArray
				&& !string.IsNullOrEmpty(variable.VariableName))
			{
				candidates.Add(variable.VariableName);
			}
		}

		StatescriptNodeProperty? binding = FindBinding(StatescriptPropertyDirection.Output, index);
		string? current = binding?.Resolver is VariableResolverResource resolver ? resolver.VariableName : null;

		if (!string.IsNullOrEmpty(current) && !candidates.Contains(current))
		{
			current = null;
			RemoveBinding(StatescriptPropertyDirection.Output, index);
		}

		AddOutputVariableBadgeRow(
			container,
			label,
			$"_fold_output_obj_{index}",
			candidates,
			current,
			variableName => OnObjectOutputSelected(variableName, index, objectTypeId));
	}

	private void OnObjectOutputSelected(string? variableName, int index, string objectTypeId)
	{
		VariableResolverResource? newResolver = string.IsNullOrEmpty(variableName)
			? null
			: new VariableResolverResource
			{
				VariableName = variableName,
				Scope = VariableScope.Graph,
				ObjectTypeId = objectTypeId,
				IsArray = false,
			};

		ApplyOutputVariableBinding(index, newResolver, "Change Output Variable");
	}

	private string ReadStringConfig(string key, string defaultValue)
	{
		return NodeResource.CustomData.TryGetValue(key, out Variant value) ? value.AsString() : defaultValue;
	}

	private bool ReadBoolConfig(string key, bool defaultValue)
	{
		return NodeResource.CustomData.TryGetValue(key, out Variant value) ? value.AsBool() : defaultValue;
	}
}
#endif
