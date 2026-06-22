// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Godot;
using GodotCollections = Godot.Collections;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

public partial class StatescriptGraphNode
{
	private readonly Dictionary<PropertySlotKey, InputPropertyContext> _inputPropertyContexts = [];

	private void AddInputPropertyRow(
		StatescriptNodeDiscovery.InputPropertyInfo propInfo,
		int index,
		Control sectionContainer,
		string? shapeCustomDataKey = null,
		string? preferredDefaultResolverTypeId = null)
	{
		if (NodeResource is null)
		{
			return;
		}

		var key = new PropertySlotKey(StatescriptPropertyDirection.Input, index);

		FoldableContainer propertyFoldable = InlineConstantSummaryFormatter.BuildColumnedFoldable(
			sectionContainer,
			propInfo.Label,
			GetFoldState(GetInputPropertyFoldKey(index), true));

		_foldableKeys[propertyFoldable] = GetInputPropertyFoldKey(index);
		_inputPropertyFoldables[key] = new InputPropertyFoldableContext(propertyFoldable, string.Empty);
		propertyFoldable.FoldingChanged += OnSectionFoldingChanged;

		var container = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		propertyFoldable.AddChild(container);

		var headerRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		headerRow.AddThemeConstantOverride("separation", 5);
		container.AddChild(headerRow);

		Action<bool>? onShapeChanged = shapeCustomDataKey is null
			? null
			: isArray => ChangeInputPropertyConfigInternal(
				index,
				new GodotCollections.Dictionary { { shapeCustomDataKey, isArray } },
				"Change Input Shape");

		OptionButton valueShapeDropdown =
			StatescriptEditorControls.CreateValueShapeDropdown(propInfo.IsArray, onShapeChanged);

		valueShapeDropdown.Disabled = shapeCustomDataKey is null;

		List<Func<NodeEditorProperty>> resolverFactories =
			StatescriptResolverRegistry.GetCompatibleFactories(propInfo.ExpectedType);

		if (propInfo.IsArray)
		{
			resolverFactories.RemoveAll(factory =>
			{
				return !StatescriptResolverRegistry.SupportsArrayValues(factory);
			});
		}
		else
		{
			resolverFactories.RemoveAll(factory =>
			{
				return !StatescriptResolverRegistry.SupportsScalarValues(factory);
			});
		}

		if (resolverFactories.Count == 0)
		{
			var errorLabel = new Label
			{
				Text = "No compatible resolvers.",
			};

			errorLabel.AddThemeColorOverride("font_color", Colors.Red);
			headerRow.AddChild(valueShapeDropdown);
			container.AddChild(errorLabel);
			UpdateInputPropertyFoldableTitle(key);
			return;
		}

		var resolverDropdown = new OptionButton
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(80, 0),
		};

		foreach (Func<NodeEditorProperty> factory in resolverFactories)
		{
			resolverDropdown.AddItem(StatescriptResolverRegistry.GetDisplayName(factory));
		}

		StatescriptNodeProperty? binding = FindBinding(StatescriptPropertyDirection.Input, index);
		int selectedIndex = 0;

		if (binding?.Resolver is not null)
		{
			for (int i = 0; i < resolverFactories.Count; i++)
			{
				if (StatescriptResolverRegistry.GetResolverTypeId(resolverFactories[i])
					== GetResolverTypeId(binding.Resolver))
				{
					selectedIndex = i;
					break;
				}
			}
		}
		else
		{
			selectedIndex = -1;

			if (!string.IsNullOrEmpty(preferredDefaultResolverTypeId))
			{
				for (int i = 0; i < resolverFactories.Count; i++)
				{
					if (StatescriptResolverRegistry.GetResolverTypeId(resolverFactories[i])
						== preferredDefaultResolverTypeId)
					{
						selectedIndex = i;
						break;
					}
				}
			}

			if (selectedIndex < 0)
			{
				selectedIndex = StatescriptResolverRegistry.GetDefaultFactoryIndex(
					resolverFactories,
					propInfo.ExpectedType,
					propInfo.IsArray);
			}
		}

		resolverDropdown.Selected = selectedIndex;
		headerRow.AddChild(resolverDropdown);
		headerRow.AddChild(valueShapeDropdown);

		var editorContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		container.AddChild(editorContainer);

		_inputPropertyContexts[key] = new InputPropertyContext(resolverFactories, propInfo, editorContainer);

		ShowResolverEditorUI(
			resolverFactories[selectedIndex],
			binding,
			propInfo.ExpectedType,
			editorContainer,
			StatescriptPropertyDirection.Input,
			index,
			propInfo.IsArray);

		// Persist the default resolver binding for a fresh input slot so the value shown in the editor is the value
		// used at runtime, without requiring the user to interact with the slot first.
		if (binding?.Resolver is null
			&& _activeResolverEditors.TryGetValue(key, out NodeEditorProperty? defaultEditor))
		{
			defaultEditor.SaveTo(EnsureBinding(StatescriptPropertyDirection.Input, index));
			NotifyGraphResourceChanged();
		}

		UpdateInputPropertyFoldableTitle(key);

		int capturedIndex = index;
		resolverDropdown.ItemSelected +=
			selectedItem => OnInputResolverDropdownItemSelected(selectedItem, capturedIndex);
	}

	private void OnInputResolverDropdownItemSelected(long x, int index)
	{
		var key = new PropertySlotKey(StatescriptPropertyDirection.Input, index);

		if (!_inputPropertyContexts.TryGetValue(key, out InputPropertyContext? ctx))
		{
			return;
		}

		var oldResolver = FindBinding(StatescriptPropertyDirection.Input, index)?.Resolver?.Duplicate()
			as StatescriptResolverResource;

		if (_activeResolverEditors.TryGetValue(key, out NodeEditorProperty? old))
		{
			_activeResolverEditors.Remove(key);
		}

		ClearContainer(ctx.EditorContainer);

		if (NodeResource is null)
		{
			return;
		}

		ShowResolverEditorUI(
			ctx.ResolverFactories[(int)x],
			null,
			ctx.PropInfo.ExpectedType,
			ctx.EditorContainer,
			StatescriptPropertyDirection.Input,
			index,
			ctx.PropInfo.IsArray);

		if (_activeResolverEditors.TryGetValue(key, out NodeEditorProperty? editor))
		{
			SaveResolverEditor(editor, StatescriptPropertyDirection.Input, index);
		}

		UpdateInputPropertyFoldableTitle(key);

		StatescriptNodeProperty? updated = FindBinding(StatescriptPropertyDirection.Input, index);
		var newResolver = updated?.Resolver?.Duplicate() as StatescriptResolverResource;

		EditorUndoRedoUtils.Record(
			_undoRedo,
			"Change Resolver Type",
			_graph,
			undo =>
			{
				undo.AddDoMethod(
					this,
					MethodName.ApplyResolverBinding,
					(int)StatescriptPropertyDirection.Input,
					index,
					Variant.From(newResolver));
				undo.AddUndoMethod(
					this,
					MethodName.ApplyResolverBinding,
					(int)StatescriptPropertyDirection.Input,
					index,
					Variant.From(oldResolver));
			});

		PropertyBindingChanged?.Invoke();
		ResetSize();
	}

	private void AddOutputVariableRow(
		StatescriptNodeDiscovery.OutputVariableInfo varInfo,
		int index,
		FoldableContainer sectionContainer)
	{
		if (NodeResource is null || _graph is null)
		{
			return;
		}

		string foldKey = $"_fold_output_{index}";
		FoldableContainer foldable = InlineConstantSummaryFormatter.BuildColumnedFoldable(
			sectionContainer,
			varInfo.Label,
			GetFoldState(foldKey, true));

		var variableDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		variableDropdown.SetMeta("is_variable_dropdown", true);
		variableDropdown.SetMeta("output_index", index);

		foreach (StatescriptGraphVariable v in _graph.Variables)
		{
			variableDropdown.AddItem(v.VariableName);
		}

		StatescriptNodeProperty? binding = FindBinding(StatescriptPropertyDirection.Output, index);
		int selectedIndex = 0;

		if (binding?.Resolver is VariableResolverResource varRes
			&& !string.IsNullOrEmpty(varRes.VariableName))
		{
			for (int i = 0; i < _graph.Variables.Count; i++)
			{
				if (_graph.Variables[i].VariableName == varRes.VariableName)
				{
					selectedIndex = i;
					break;
				}
			}
		}

		if (_graph.Variables.Count > 0)
		{
			variableDropdown.Selected = selectedIndex;

			if (binding is null)
			{
				StatescriptGraphVariable variable = _graph.Variables[selectedIndex];
				EnsureBinding(StatescriptPropertyDirection.Output, index).Resolver =
					new VariableResolverResource
					{
						VariableName = variable.VariableName,
						Scope = VariableScope.Graph,
						VariableType = variable.VariableType,
						IsArray = variable.IsArray,
					};
				NotifyGraphResourceChanged();
			}
		}

		void UpdateOutputVariableBadge()
		{
			string selectedName = _graph.Variables.Count > 0 && variableDropdown.Selected >= 0
				? variableDropdown.GetItemText(variableDropdown.Selected)
				: string.Empty;

			InlineConstantSummaryFormatter.ApplyFoldableTitle(
				string.Empty,
				foldable,
				string.IsNullOrEmpty(selectedName) ? "(None)" : selectedName,
				InlineSummaryBadgeKind.Variable,
				highlightedVariableName: selectedName);
		}

		int capturedIndex = index;
		variableDropdown.ItemSelected += selectedItem =>
		{
			OnOutputVariableDropdownItemSelected(selectedItem, capturedIndex);
			UpdateOutputVariableBadge();
		};

		foldable.AddChild(variableDropdown);

		foldable.FoldingChanged += folded =>
		{
			SetFoldStateWithUndo(foldKey, folded);
			UpdateOutputVariableBadge();
			ResetSize();
		};

		UpdateOutputVariableBadge();
	}

	private void OnOutputVariableDropdownItemSelected(long x, int index)
	{
		if (NodeResource is null || _graph is null)
		{
			return;
		}

		var oldResolver = FindBinding(StatescriptPropertyDirection.Output, index)?.Resolver?.Duplicate()
			as StatescriptResolverResource;

		StatescriptGraphVariable variable = _graph.Variables[(int)x];
		var newResolver = new VariableResolverResource
		{
			VariableName = variable.VariableName,
			Scope = VariableScope.Graph,
		};
		EnsureBinding(StatescriptPropertyDirection.Output, index).Resolver = newResolver;
		NotifyGraphResourceChanged();

		EditorUndoRedoUtils.Record(
			_undoRedo,
			"Change Output Variable",
			_graph,
			undo =>
			{
				undo.AddDoMethod(
					this,
					MethodName.ApplyResolverBinding,
					(int)StatescriptPropertyDirection.Output,
					index,
					(StatescriptResolverResource)newResolver.Duplicate());
				undo.AddUndoMethod(
					this,
					MethodName.ApplyResolverBinding,
					(int)StatescriptPropertyDirection.Output,
					index,
					Variant.From(oldResolver));
			});

		PropertyBindingChanged?.Invoke();
	}

	private void ShowResolverEditorUI(
		Func<NodeEditorProperty> factory,
		StatescriptNodeProperty? existingBinding,
		Type expectedType,
		VBoxContainer container,
		StatescriptPropertyDirection direction,
		int propertyIndex,
		bool isArray = false)
	{
		if (_graph is null)
		{
			return;
		}

		NodeEditorProperty resolverEditor = factory();
		resolverEditor.ConfigureAllowedExpectedTypes(expectedType);

		var key = new PropertySlotKey(direction, propertyIndex);

		resolverEditor.Setup(
			_graph,
			existingBinding,
			expectedType,
			() => SaveResolverEditorWithUndo(resolverEditor, direction, propertyIndex),
			isArray);

		resolverEditor.LayoutSizeChanged += ResetSize;

		container.AddChild(resolverEditor);

		_activeResolverEditors[key] = resolverEditor;
	}

	private void SaveResolverEditorWithUndo(
		NodeEditorProperty resolverEditor,
		StatescriptPropertyDirection direction,
		int propertyIndex)
	{
		if (NodeResource is null)
		{
			return;
		}

		StatescriptNodeProperty? existing = FindBinding(direction, propertyIndex);
		var oldResolver = existing?.Resolver?.Duplicate() as StatescriptResolverResource;

		SaveResolverEditor(resolverEditor, direction, propertyIndex);

		StatescriptNodeProperty? updated = FindBinding(direction, propertyIndex);
		var newResolver = updated?.Resolver?.Duplicate() as StatescriptResolverResource;

		EditorUndoRedoUtils.Record(
			_undoRedo,
			"Change Node Property",
			_graph,
			undo =>
			{
				undo.AddDoMethod(
					this,
					MethodName.ApplyResolverBinding,
					(int)direction,
					propertyIndex,
					Variant.From(newResolver));
				undo.AddUndoMethod(
					this,
					MethodName.ApplyResolverBinding,
					(int)direction,
					propertyIndex,
					Variant.From(oldResolver));
			});

		if (direction == StatescriptPropertyDirection.Input)
		{
			UpdateInputPropertyFoldableTitle(new PropertySlotKey(direction, propertyIndex));
		}

		PropertyBindingChanged?.Invoke();
		ResetSize();
	}

	private void SaveResolverEditor(
		NodeEditorProperty resolverEditor,
		StatescriptPropertyDirection direction,
		int propertyIndex)
	{
		if (NodeResource is null)
		{
			return;
		}

		StatescriptNodeProperty binding = EnsureBinding(direction, propertyIndex);
		resolverEditor.SaveTo(binding);
		NotifyGraphResourceChanged();
	}

	private sealed class InputPropertyContext(
		List<Func<NodeEditorProperty>> resolverFactories,
		StatescriptNodeDiscovery.InputPropertyInfo propInfo,
		VBoxContainer editorContainer)
	{
		public List<Func<NodeEditorProperty>> ResolverFactories { get; } = resolverFactories;

		public StatescriptNodeDiscovery.InputPropertyInfo PropInfo { get; } = propInfo;

		public VBoxContainer EditorContainer { get; } = editorContainer;
	}
}
#endif
