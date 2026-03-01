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
			for (var i = 0; i < resolverFactories.Count; i++)
			{
				using NodeEditorProperty temp = resolverFactories[i]();

				if (temp.ResolverTypeId == "Variant")
				{
					selectedIndex = i;
					break;
				}
			}
		}

		resolverDropdown.Selected = selectedIndex;
		headerRow.AddChild(resolverDropdown);

		var editorContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		container.AddChild(editorContainer);

		ShowResolverEditorUI(
			resolverFactories[selectedIndex],
			binding,
			propInfo.ExpectedType,
			editorContainer,
			StatescriptPropertyDirection.Input,
			index,
			propInfo.IsArray);

		resolverDropdown.ItemSelected += x =>
		{
			var key = new PropertySlotKey(StatescriptPropertyDirection.Input, index);

			var oldResolver = FindBinding(StatescriptPropertyDirection.Input, index)?.Resolver?.Duplicate()
				as StatescriptResolverResource;

			if (_activeResolverEditors.TryGetValue(key, out NodeEditorProperty? old))
			{
				_activeResolverEditors.Remove(key);
			}

			ClearContainer(editorContainer);

			if (NodeResource is null)
			{
				return;
			}

			ShowResolverEditorUI(
				resolverFactories[(int)x],
				null,
				propInfo.ExpectedType,
				editorContainer,
				StatescriptPropertyDirection.Input,
				index,
				propInfo.IsArray);

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
					newResolver ?? new StatescriptResolverResource());
				_undoRedo.AddUndoMethod(
					this,
					MethodName.ApplyResolverBinding,
					(int)StatescriptPropertyDirection.Input,
					index,
					oldResolver ?? new StatescriptResolverResource());
				_undoRedo.CommitAction(false);
			}

			PropertyBindingChanged?.Invoke();
			ResetSize();
		};
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

		variableDropdown.ItemSelected += x =>
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
					oldResolver ?? new StatescriptResolverResource());
				_undoRedo.CommitAction(false);
			}

			PropertyBindingChanged?.Invoke();
		};

		hBox.AddChild(variableDropdown);
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

		resolverEditor.Setup(
			_graph,
			existingBinding,
			expectedType,
			() =>
			{
				SaveResolverEditorWithUndo(resolverEditor, direction, propertyIndex);
			},
			isArray);

		resolverEditor.LayoutSizeChanged += ResetSize;

		container.AddChild(resolverEditor);

		var key = new PropertySlotKey(direction, propertyIndex);
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
				newResolver ?? new StatescriptResolverResource());
			_undoRedo.AddUndoMethod(
				this,
				MethodName.ApplyResolverBinding,
				(int)direction,
				propertyIndex,
				oldResolver ?? new StatescriptResolverResource());
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
}
#endif
