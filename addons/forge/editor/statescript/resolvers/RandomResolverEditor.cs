// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;
using GodotVector2 = Godot.Vector2;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class RandomResolverEditor : NodeEditorProperty
{
	private StatescriptGraph? _graph;
	private Action? _onChanged;
	private Type _expectedType = typeof(ForgeVariant128);
	private OptionButton? _typeDropdown;
	private OptionButton? _minResolverDropdown;
	private OptionButton? _maxResolverDropdown;
	private VBoxContainer? _minEditorContainer;
	private VBoxContainer? _maxEditorContainer;
	private NodeEditorProperty? _minEditor;
	private NodeEditorProperty? _maxEditor;
	private StatescriptVariableType _valueType = StatescriptVariableType.Int;
	private List<Func<NodeEditorProperty>> _factories = [];

	public override string DisplayName => "Random";

	public override string ResolverTypeId => "Random";

	public override bool IsCompatibleWith(Type expectedType)
	{
		if (expectedType == typeof(ForgeVariant128))
		{
			return true;
		}

		return expectedType == typeof(int)
			|| expectedType == typeof(float)
			|| expectedType == typeof(double);
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
		_expectedType = expectedType;
		var existing = property?.Resolver as RandomResolverResource;
		_valueType = existing?.ValueType ?? GetDefaultValueType(expectedType);
		_factories = ResolverEditorFactoryCatalog.GetCompatibleFactories(GetBroadExpectedClrType(_valueType));

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		var allowTypeSelection = expectedType == typeof(ForgeVariant128);
		if (allowTypeSelection)
		{
			var typeRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			root.AddChild(typeRow);
			typeRow.AddChild(new Label
			{
				Text = "Type:",
				CustomMinimumSize = new GodotVector2(45, 0),
				HorizontalAlignment = HorizontalAlignment.Right,
			});

			_typeDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			_typeDropdown.AddItem("Int");
			_typeDropdown.AddItem("Float");
			_typeDropdown.AddItem("Double");
			_typeDropdown.Selected = GetTypeDropdownIndex(_valueType);
			_typeDropdown.ItemSelected += OnTypeDropdownItemSelected;
			typeRow.AddChild(_typeDropdown);
		}

		BuildResolverSlot(root, "Min:", existing?.Left, out _minResolverDropdown, out _minEditorContainer, x => _minEditor = x, OnMinResolverDropdownItemSelected);
		BuildResolverSlot(root, "Max:", existing?.Right, out _maxResolverDropdown, out _maxEditorContainer, x => _maxEditor = x, OnMaxResolverDropdownItemSelected);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new RandomResolverResource
		{
			ValueType = _valueType,
			Left = SaveNestedEditor(_minEditor),
			Right = SaveNestedEditor(_maxEditor),
		};
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_minEditor?.ClearCallbacks();
		_maxEditor?.ClearCallbacks();
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

	private static Type GetBroadExpectedClrType(StatescriptVariableType valueType)
	{
		return valueType switch
		{
			StatescriptVariableType.Float => typeof(float),
			StatescriptVariableType.Double => typeof(double),
			_ => typeof(int),
		};
	}

	private static StatescriptVariableType GetDefaultValueType(Type expectedType)
	{
		if (expectedType == typeof(float))
		{
			return StatescriptVariableType.Float;
		}

		if (expectedType == typeof(double))
		{
			return StatescriptVariableType.Double;
		}

		return StatescriptVariableType.Int;
	}

	private static int GetTypeDropdownIndex(StatescriptVariableType valueType)
	{
		return valueType switch
		{
			StatescriptVariableType.Float => 1,
			StatescriptVariableType.Double => 2,
			_ => 0,
		};
	}

	private void BuildResolverSlot(
		VBoxContainer root,
		string title,
		StatescriptResolverResource? existingResolver,
		out OptionButton resolverDropdown,
		out VBoxContainer editorContainer,
		Action<NodeEditorProperty?> setEditor,
		Action<long> onSelected)
	{
		FoldableContainer foldable = new() { Title = title };
		foldable.FoldingChanged += _ => RaiseLayoutSizeChanged();
		root.AddChild(foldable);

		var container = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		foldable.AddChild(container);

		resolverDropdown = CreateResolverDropdown(existingResolver);
		editorContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		container.AddChild(resolverDropdown);
		container.AddChild(editorContainer);
		ShowEditor(GetSelectedIndex(existingResolver), existingResolver, editorContainer, setEditor);
		resolverDropdown.ItemSelected += index => onSelected(index);
	}

	private void OnTypeDropdownItemSelected(long index)
	{
		_valueType = index switch
		{
			1 => StatescriptVariableType.Float,
			2 => StatescriptVariableType.Double,
			_ => StatescriptVariableType.Int,
		};

		_factories = ResolverEditorFactoryCatalog.GetCompatibleFactories(GetBroadExpectedClrType(_valueType));
		RebuildResolverSlot(_minEditorContainer, x => _minEditor = x);
		RebuildResolverSlot(_maxEditorContainer, x => _maxEditor = x);
		_onChanged?.Invoke();
		RaiseLayoutSizeChanged();
	}

	private void OnMinResolverDropdownItemSelected(long index)
	{
		HandleResolverChanged((int)index, _minEditorContainer, x => _minEditor = x);
	}

	private void OnMaxResolverDropdownItemSelected(long index)
	{
		HandleResolverChanged((int)index, _maxEditorContainer, x => _maxEditor = x);
	}

	private void RebuildResolverSlot(VBoxContainer? editorContainer, Action<NodeEditorProperty?> setEditor)
	{
		if (editorContainer?.GetParent() is not VBoxContainer slotContainer)
		{
			return;
		}

		if (slotContainer.GetChildCount() == 0 || slotContainer.GetChild(0) is not OptionButton dropdown)
		{
			return;
		}

		dropdown.Clear();
		foreach (Func<NodeEditorProperty> factory in _factories)
		{
			using NodeEditorProperty temp = factory();
			dropdown.AddItem(temp.DisplayName);
		}

		dropdown.Selected = 0;
		HandleResolverChanged(0, editorContainer, setEditor);
	}

	private void HandleResolverChanged(
		int selectedIndex,
		VBoxContainer? editorContainer,
		Action<NodeEditorProperty?> setEditor)
	{
		if (editorContainer is null)
		{
			return;
		}

		foreach (Node child in editorContainer.GetChildren())
		{
			editorContainer.RemoveChild(child);
			child.Free();
		}

		setEditor(null);
		ShowEditor(selectedIndex, null, editorContainer, setEditor);
		_onChanged?.Invoke();
		RaiseLayoutSizeChanged();
	}

	private int GetSelectedIndex(StatescriptResolverResource? existingResolver)
	{
		return ResolverEditorFactoryCatalog.GetDefaultFactoryIndex(_factories, existingResolver, "Variant");
	}

	private OptionButton CreateResolverDropdown(StatescriptResolverResource? existingResolver)
	{
		var dropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		foreach (Func<NodeEditorProperty> factory in _factories)
		{
			using NodeEditorProperty temp = factory();
			dropdown.AddItem(temp.DisplayName);
		}

		dropdown.Selected = GetSelectedIndex(existingResolver);
		return dropdown;
	}

	private void ShowEditor(
		int factoryIndex,
		StatescriptResolverResource? existingResolver,
		VBoxContainer container,
		Action<NodeEditorProperty?> setEditor)
	{
		if (_graph is null || factoryIndex < 0 || factoryIndex >= _factories.Count)
		{
			return;
		}

		NodeEditorProperty editor = _factories[factoryIndex]();
		StatescriptNodeProperty? tempProperty = existingResolver is null ? null : new StatescriptNodeProperty { Resolver = existingResolver };
		editor.Setup(_graph, tempProperty, GetExpectedClrType(_valueType), OnNestedEditorChanged, false);
		editor.LayoutSizeChanged += RaiseLayoutSizeChanged;
		container.AddChild(editor);
		setEditor(editor);
	}

	private void OnNestedEditorChanged()
	{
		_onChanged?.Invoke();
	}

	private Type GetExpectedClrType(StatescriptVariableType valueType)
	{
		if (_expectedType == typeof(ForgeVariant128))
		{
			return GetBroadExpectedClrType(valueType);
		}

		return _expectedType;
	}
}
#endif
