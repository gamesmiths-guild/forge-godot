// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

public partial class StatescriptGraphNode
{
	private readonly Dictionary<PropertySlotKey, InputPropertyContext> _inputPropertyContexts = [];

	private void AddInputPropertyRow(
		StatescriptNodeDiscovery.InputPropertyInfo propInfo,
		int index,
		Control sectionContainer)
	{
		if (NodeResource is null)
		{
			return;
		}

		var container = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		sectionContainer.AddChild(container);

		var headerRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		container.AddChild(headerRow);

		var nameLabel = new Label
		{
			Text = propInfo.Label,
			CustomMinimumSize = new Vector2(60, 0),
		};

		nameLabel.AddThemeColorOverride("font_color", _inputPropertyColor);
		headerRow.AddChild(nameLabel);

		List<Func<NodeEditorProperty>> resolverFactories =
			StatescriptResolverRegistry.GetCompatibleFactories(propInfo.ExpectedType);

		if (propInfo.IsArray)
		{
			resolverFactories.RemoveAll(factory =>
			{
				using NodeEditorProperty temp = factory();
				return temp.ResolverTypeId != "ArrayVariable";
			});
		}
		else
		{
			resolverFactories.RemoveAll(factory =>
			{
				using NodeEditorProperty temp = factory();
				return temp.ResolverTypeId == "ArrayVariable";
			});
		}

		if (resolverFactories.Count == 0)
		{
			var errorLabel = new Label
			{
				Text = "No compatible resolvers.",
			};

			errorLabel.AddThemeColorOverride("font_color", Colors.Red);
			headerRow.AddChild(errorLabel);
			return;
		}

		var resolverDropdown = new OptionButton
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(80, 0),
		};

		foreach (Func<NodeEditorProperty> factory in resolverFactories)
		{
			using NodeEditorProperty temp = factory();
			resolverDropdown.AddItem(temp.DisplayName);
		}

		StatescriptNodeProperty? binding = FindBinding(StatescriptPropertyDirection.Input, index);
		var selectedIndex = 0;

		if (binding?.Resolver is not null)
		{
			for (var i = 0; i < resolverFactories.Count; i++)
			{
				using NodeEditorProperty temp = resolverFactories[i]();

				if (temp.ResolverTypeId == GetResolverTypeId(binding.Resolver))
				{
					selectedIndex = i;
					break;
				}
			}
		}
		else
		{
			selectedIndex = StatescriptResolverRegistry.GetDefaultFactoryIndex(resolverFactories, propInfo.IsArray);
		}

		resolverDropdown.Selected = selectedIndex;
		headerRow.AddChild(resolverDropdown);

		var editorContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		container.AddChild(editorContainer);

		var key = new PropertySlotKey(StatescriptPropertyDirection.Input, index);
		_inputPropertyContexts[key] = new InputPropertyContext(resolverFactories, propInfo, editorContainer);

		ShowResolverEditorUI(
			resolverFactories[selectedIndex],
			binding,
			propInfo.ExpectedType,
			editorContainer,
			StatescriptPropertyDirection.Input,
			index,
			propInfo.IsArray);

		var capturedIndex = index;
		resolverDropdown.ItemSelected += selectedItem => OnInputResolverDropdownItemSelected(selectedItem, capturedIndex);
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

		StatescriptNodeProperty? updated = FindBinding(StatescriptPropertyDirection.Input, index);
		var newResolver = updated?.Resolver?.Duplicate() as StatescriptResolverResource;

		if (_undoRedo is not null)
		{
			_undoRedo.CreateAction("Change Resolver Type", customContext: _graph);

			_undoRedo.AddDoMethod(
				this,
				MethodName.ApplyResolverBinding,
				(int)StatescriptPropertyDirection.Input,
				index,
				Variant.From(newResolver));

			_undoRedo.AddUndoMethod(
				this,
				MethodName.ApplyResolverBinding,
				(int)StatescriptPropertyDirection.Input,
				index,
				Variant.From(oldResolver));

			_undoRedo.CommitAction(false);
		}

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

		var hBox = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		sectionContainer.AddChild(hBox);

		var nameLabel = new Label
		{
			Text = varInfo.Label,
			CustomMinimumSize = new Vector2(60, 0),
		};

		nameLabel.AddThemeColorOverride("font_color", _outputVariableColor);
		hBox.AddChild(nameLabel);

		var variableDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		variableDropdown.SetMeta("is_variable_dropdown", true);
		variableDropdown.SetMeta("output_index", index);

		foreach (StatescriptGraphVariable v in _graph.Variables)
		{
			variableDropdown.AddItem(v.VariableName);
		}

		StatescriptNodeProperty? binding = FindBinding(StatescriptPropertyDirection.Output, index);
		var selectedIndex = 0;

		if (binding?.Resolver is VariableResolverResource varRes
			&& !string.IsNullOrEmpty(varRes.VariableName))
		{
			for (var i = 0; i < _graph.Variables.Count; i++)
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
				var variableName = _graph.Variables[selectedIndex].VariableName;
				EnsureBinding(StatescriptPropertyDirection.Output, index).Resolver =
					new VariableResolverResource { VariableName = variableName };
			}
		}

		var capturedIndex = index;
		variableDropdown.ItemSelected += selectedItem => OnOutputVariableDropdownItemSelected(selectedItem, capturedIndex);

		hBox.AddChild(variableDropdown);
	}

	private void OnOutputVariableDropdownItemSelected(long x, int index)
	{
		if (NodeResource is null || _graph is null)
		{
			return;
		}

		var oldResolver = FindBinding(StatescriptPropertyDirection.Output, index)?.Resolver?.Duplicate()
			as StatescriptResolverResource;

		var variableName = _graph.Variables[(int)x].VariableName;
		var newResolver = new VariableResolverResource { VariableName = variableName };
		EnsureBinding(StatescriptPropertyDirection.Output, index).Resolver = newResolver;

		if (_undoRedo is not null)
		{
			_undoRedo.CreateAction("Change Output Variable", customContext: _graph);

			_undoRedo.AddDoMethod(
				this,
				MethodName.ApplyResolverBinding,
				(int)StatescriptPropertyDirection.Output,
				index,
				(StatescriptResolverResource)newResolver.Duplicate());

			_undoRedo.AddUndoMethod(
				this,
				MethodName.ApplyResolverBinding,
				(int)StatescriptPropertyDirection.Output,
				index,
				Variant.From(oldResolver));

			_undoRedo.CommitAction(false);
		}

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

		if (_undoRedo is not null)
		{
			_undoRedo.CreateAction("Change Node Property", customContext: _graph);

			_undoRedo.AddDoMethod(
				this,
				MethodName.ApplyResolverBinding,
				(int)direction,
				propertyIndex,
				Variant.From(newResolver));

			_undoRedo.AddUndoMethod(
				this,
				MethodName.ApplyResolverBinding,
				(int)direction,
				propertyIndex,
				Variant.From(oldResolver));

			_undoRedo.CommitAction(false);
		}

		PropertyBindingChanged?.Invoke();
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
