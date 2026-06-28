// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;
using GodotVector2 = Godot.Vector2;
using SysVector2 = System.Numerics.Vector2;
using SysVector3 = System.Numerics.Vector3;
using SysVector4 = System.Numerics.Vector4;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class VectorComponentResolverEditor : NodeEditorProperty
{
	private StatescriptGraph? _graph;
	private Action? _onChanged;
	private VBoxContainer? _editorContainer;
	private NodeEditorProperty? _operandEditor;
	private FoldableContainer? _operandFoldable;
	private OptionButton? _resolverDropdown;
	private OptionButton? _typeDropdown;
	private OptionButton? _componentDropdown;
	private List<Func<NodeEditorProperty>> _factories = [];
	private StatescriptVariableType _operandType = StatescriptVariableType.Vector3;
	private VectorComponent _component = VectorComponent.X;

	public override string DisplayName => "Vector Component";

	public override string ResolverTypeId => "VectorComponent";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(float) || expectedType == typeof(ForgeVariant128);
	}

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		_graph = graph;
		_onChanged = onChanged;
		var existing = property?.Resolver as VectorComponentResolverResource;
		_operandType = existing?.OperandType ?? StatescriptVariableType.Vector3;
		_component = existing?.Component ?? VectorComponent.X;
		_factories = ResolverEditorFactoryCatalog.GetCompatibleFactories(GetOperandClrType());

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		var typeRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		root.AddChild(typeRow);
		typeRow.AddChild(new Label
		{
			Text = "Type:",
			CustomMinimumSize = new GodotVector2(45, 0),
			HorizontalAlignment = HorizontalAlignment.Right,
		});

		_typeDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_typeDropdown.AddItem("Vector2");
		_typeDropdown.AddItem("Vector3");
		_typeDropdown.AddItem("Vector4");
		_typeDropdown.Selected = _operandType switch
		{
			StatescriptVariableType.Vector2 => 0,
			StatescriptVariableType.Vector4 => 2,
			_ => 1,
		};
		_typeDropdown.ItemSelected += OnTypeDropdownItemSelected;
		typeRow.AddChild(_typeDropdown);

		_operandFoldable = InlineConstantSummaryFormatter.BuildColumnedFoldable(
			root,
			"Vector:",
			existing?.OperandFolded ?? true);
		_operandFoldable.FoldingChanged += OnOperandFoldableFoldingChanged;
		var container = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_operandFoldable.AddChild(container);

		_resolverDropdown = CreateResolverDropdown(existing?.Operand);
		_editorContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		container.AddChild(NestedResolverEditorUtilities.CreateResolverSelectorRow(_resolverDropdown));
		container.AddChild(_editorContainer);
		ShowEditor(GetSelectedIndex(existing?.Operand), existing?.Operand);
		_resolverDropdown.ItemSelected += OnResolverDropdownItemSelected;

		var componentRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		root.AddChild(componentRow);
		componentRow.AddChild(new Label
		{
			Text = "Comp:",
			CustomMinimumSize = new GodotVector2(45, 0),
			HorizontalAlignment = HorizontalAlignment.Right,
		});

		_componentDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		PopulateComponentDropdown();
		_componentDropdown.ItemSelected += OnComponentDropdownItemSelected;
		componentRow.AddChild(_componentDropdown);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new VectorComponentResolverResource
		{
			OperandType = _operandType,
			Component = _component,
			OperandFolded = _operandFoldable?.Folded ?? false,
			Operand = SaveNestedEditor(_operandEditor),
		};
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_operandEditor?.ClearCallbacks();
		_operandEditor = null;
		_editorContainer = null;
		_operandFoldable = null;
		_resolverDropdown = null;
		_typeDropdown = null;
		_componentDropdown = null;
	}

	private static StatescriptResolverResource? SaveNestedEditor(NodeEditorProperty? editor)
	{
		if (editor is null)
		{
			return null;
		}

		var property = new StatescriptNodeProperty();
		editor.SaveTo(property);
		return property.Resolver;
	}

	private void OnTypeDropdownItemSelected(long index)
	{
		_operandType = index switch
		{
			0 => StatescriptVariableType.Vector2,
			2 => StatescriptVariableType.Vector4,
			_ => StatescriptVariableType.Vector3,
		};

		_factories = ResolverEditorFactoryCatalog.GetCompatibleFactories(GetOperandClrType());
		PopulateResolverDropdown();
		PopulateComponentDropdown();
		_onChanged?.Invoke();
		RaiseLayoutSizeChanged();
	}

	private void OnOperandFoldableFoldingChanged(bool folded)
	{
		UpdateOperandFoldableTitle();
		_onChanged?.Invoke();
		RaiseLayoutSizeChanged();
	}

	private void OnResolverDropdownItemSelected(long index)
	{
		if (_editorContainer is null)
		{
			return;
		}

		foreach (Node child in _editorContainer.GetChildren())
		{
			_editorContainer.RemoveChild(child);
			child.Free();
		}

		_operandEditor = null;
		ShowEditor((int)index, null);
		_onChanged?.Invoke();
		RaiseLayoutSizeChanged();
	}

	private void OnComponentDropdownItemSelected(long index)
	{
		_component = (VectorComponent)(int)index;
		_onChanged?.Invoke();
	}

	private void PopulateResolverDropdown()
	{
		if (_resolverDropdown is null)
		{
			return;
		}

		_resolverDropdown.Clear();
		foreach (Func<NodeEditorProperty> factory in _factories)
		{
			_resolverDropdown.AddItem(StatescriptResolverRegistry.GetDisplayName(factory));
		}

		int selectedIndex = GetSelectedIndex(null);
		_resolverDropdown.Selected = selectedIndex;

		if (_editorContainer is not null)
		{
			foreach (Node child in _editorContainer.GetChildren())
			{
				_editorContainer.RemoveChild(child);
				child.Free();
			}

			ShowEditor(selectedIndex, null);
		}
	}

	private void PopulateComponentDropdown()
	{
		if (_componentDropdown is null)
		{
			return;
		}

		_componentDropdown.Clear();
		_componentDropdown.AddItem("X");
		_componentDropdown.AddItem("Y");

		if (_operandType != StatescriptVariableType.Vector2)
		{
			_componentDropdown.AddItem("Z");
		}

		if (_operandType == StatescriptVariableType.Vector4)
		{
			_componentDropdown.AddItem("W");
		}

		int maxIndex = _operandType switch
		{
			StatescriptVariableType.Vector2 => 1,
			StatescriptVariableType.Vector3 => 2,
			_ => 3,
		};

		int selected = Math.Clamp((int)_component, 0, maxIndex);
		_component = (VectorComponent)selected;
		_componentDropdown.Selected = selected;
	}

	private int GetSelectedIndex(StatescriptResolverResource? existingResolver)
	{
		return ResolverEditorFactoryCatalog.GetDefaultFactoryIndex(_factories, existingResolver, "Variant");
	}

	private OptionButton CreateResolverDropdown(StatescriptResolverResource? existingResolver)
	{
		OptionButton dropdown = new SearchableOptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		foreach (Func<NodeEditorProperty> factory in _factories)
		{
			dropdown.AddItem(StatescriptResolverRegistry.GetDisplayName(factory));
		}

		dropdown.Selected = GetSelectedIndex(existingResolver);
		return dropdown;
	}

	private void ShowEditor(int factoryIndex, StatescriptResolverResource? existingResolver)
	{
		if (_graph is null || _editorContainer is null || factoryIndex < 0 || factoryIndex >= _factories.Count)
		{
			return;
		}

		NodeEditorProperty editor = _factories[factoryIndex]();
		StatescriptNodeProperty? tempProperty =
			existingResolver is null ? null : new StatescriptNodeProperty { Resolver = existingResolver };
		editor.Setup(_graph, tempProperty, GetOperandClrType(), OnNestedEditorChanged, false);
		editor.LayoutSizeChanged += RaiseLayoutSizeChanged;
		_editorContainer.AddChild(editor);
		_operandEditor = editor;
		UpdateOperandFoldableTitle();
	}

	private void OnNestedEditorChanged()
	{
		UpdateOperandFoldableTitle();
		_onChanged?.Invoke();
	}

	private void UpdateOperandFoldableTitle()
	{
		if (_operandFoldable is not null)
		{
			InlineConstantSummaryFormatter.ApplyFoldableTitle("Vector:", _operandFoldable, _operandEditor);
		}
	}

	private Type GetOperandClrType()
	{
		return _operandType switch
		{
			StatescriptVariableType.Vector2 => typeof(SysVector2),
			StatescriptVariableType.Vector4 => typeof(SysVector4),
			_ => typeof(SysVector3),
		};
	}
}
#endif
