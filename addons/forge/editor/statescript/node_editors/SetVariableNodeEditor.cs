// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Custom node editor for the <c>SetVariableNode</c>. Dynamically filters the Input (value resolver) based on the
/// selected target variable's type.
/// </summary>
internal sealed class SetVariableNodeEditor : CustomNodeEditor
{
	private const string FoldInputKey = "_fold_input";
	private const string FoldOutputKey = "_fold_output";

	private StatescriptVariableType? _resolvedType;
	private bool _resolvedIsArray;

	/// <inheritdoc/>
	public override string HandledRuntimeTypeName => "Gamesmiths.Forge.Statescript.Nodes.Action.SetVariableNode";

	/// <inheritdoc/>
	public override void BuildPropertySections(StatescriptNodeDiscovery.NodeTypeInfo typeInfo)
	{
		var inputFolded = GetFoldState(FoldInputKey);
		FoldableContainer inputContainer = AddPropertySectionDivider(
			"Input Properties",
			InputPropertyColor,
			FoldInputKey,
			inputFolded);

		var inputEditorContainer = new VBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};

		inputContainer.AddChild(inputEditorContainer);

		var outputFolded = GetFoldState(FoldOutputKey);
		FoldableContainer outputContainer = AddPropertySectionDivider(
			"Output Variables",
			OutputVariableColor,
			FoldOutputKey,
			outputFolded);

		_resolvedType = null;
		_resolvedIsArray = false;
		StatescriptNodeProperty? outputBinding = FindBinding(StatescriptPropertyDirection.Output, 0);

		if (outputBinding?.Resolver is VariableResolverResource varRes
			&& !string.IsNullOrEmpty(varRes.VariableName))
		{
			foreach (StatescriptGraphVariable v in Graph.Variables)
			{
				if (v.VariableName == varRes.VariableName)
				{
					_resolvedType = v.VariableType;
					_resolvedIsArray = v.IsArray;
					break;
				}
			}
		}

		if (typeInfo.InputPropertiesInfo.Length > 0)
		{
			RebuildInputUI(typeInfo.InputPropertiesInfo[0], inputEditorContainer);
		}

		if (typeInfo.OutputVariablesInfo.Length > 0)
		{
			AddTargetVariableRow(
				typeInfo.OutputVariablesInfo[0],
				0,
				outputContainer,
				typeInfo,
				inputEditorContainer);
		}
	}

	private void AddTargetVariableRow(
		StatescriptNodeDiscovery.OutputVariableInfo varInfo,
		int index,
		FoldableContainer sectionContainer,
		StatescriptNodeDiscovery.NodeTypeInfo typeInfo,
		VBoxContainer inputEditorContainer)
	{
		var hBox = new HBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};

		sectionContainer.AddChild(hBox);

		var nameLabel = new Label
		{
			Text = varInfo.Label,
			CustomMinimumSize = new Vector2(60, 0),
		};

		nameLabel.AddThemeColorOverride("font_color", OutputVariableColor);
		hBox.AddChild(nameLabel);

		var dropdown = new OptionButton
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};

		dropdown.AddItem("(None)");

		foreach (StatescriptGraphVariable v in Graph.Variables)
		{
			dropdown.AddItem(v.VariableName);
		}

		StatescriptNodeProperty? binding = FindBinding(StatescriptPropertyDirection.Output, index);
		var selectedIndex = 0;

		if (binding?.Resolver is VariableResolverResource varRes
			&& !string.IsNullOrEmpty(varRes.VariableName))
		{
			for (var i = 0; i < Graph.Variables.Count; i++)
			{
				if (Graph.Variables[i].VariableName == varRes.VariableName)
				{
					selectedIndex = i + 1;
					break;
				}
			}
		}

		dropdown.Selected = selectedIndex;

		if (selectedIndex == 0)
		{
			RemoveBinding(StatescriptPropertyDirection.Output, index);
		}

		dropdown.ItemSelected += x =>
		{
			var variableIndex = (int)x - 1;

			StatescriptVariableType? previousType = _resolvedType;
			var previousIsArray = _resolvedIsArray;

			if (variableIndex < 0)
			{
				RemoveBinding(StatescriptPropertyDirection.Output, index);
				_resolvedType = null;
				_resolvedIsArray = false;
			}
			else
			{
				var variableName = Graph.Variables[variableIndex].VariableName;
				EnsureBinding(StatescriptPropertyDirection.Output, index).Resolver =
					new VariableResolverResource { VariableName = variableName };

				_resolvedType = Graph.Variables[variableIndex].VariableType;
				_resolvedIsArray = Graph.Variables[variableIndex].IsArray;
			}

			if (previousType != _resolvedType || previousIsArray != _resolvedIsArray)
			{
				RemoveBinding(StatescriptPropertyDirection.Input, 0);

				var inputKey = new PropertySlotKey(StatescriptPropertyDirection.Input, 0);

				ActiveResolverEditors.Remove(inputKey);
			}

			if (typeInfo.InputPropertiesInfo.Length > 0)
			{
				RebuildInputUI(typeInfo.InputPropertiesInfo[0], inputEditorContainer);
			}

			RaisePropertyBindingChanged();
			ResetSize();
		};

		hBox.AddChild(dropdown);
	}

	private void RebuildInputUI(
		StatescriptNodeDiscovery.InputPropertyInfo propInfo,
		VBoxContainer container)
	{
		ClearContainer(container);

		if (_resolvedType is null)
		{
			var placeholder = new Label
			{
				Text = "Select target variable first",
				HorizontalAlignment = HorizontalAlignment.Center,
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			};

			placeholder.AddThemeColorOverride("font_color", new Color(1, 1, 1, 0.4f));
			container.AddChild(placeholder);
			ResetSize();
			return;
		}

		Type resolvedClrType = StatescriptVariableTypeConverter.ToSystemType(_resolvedType.Value);

		AddInputPropertyRow(
			new StatescriptNodeDiscovery.InputPropertyInfo(propInfo.Label, resolvedClrType, _resolvedIsArray),
			0,
			container);

		ResetSize();
	}
}
#endif
