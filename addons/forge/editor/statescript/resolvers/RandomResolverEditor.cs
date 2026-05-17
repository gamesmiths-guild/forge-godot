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
	private enum ResolverSlot
	{
		Min = 0,
		Max = 1,
	}

	private StatescriptGraph? _graph;
	private Action? _onChanged;
	private Type _expectedType = typeof(ForgeVariant128);
	private OptionButton? _typeDropdown;
	private FoldableContainer? _minFoldable;
	private FoldableContainer? _maxFoldable;
	private FoldableContainer? _inclusiveMaxFoldable;
	private OptionButton? _minResolverDropdown;
	private OptionButton? _maxResolverDropdown;
	private VBoxContainer? _minEditorContainer;
	private VBoxContainer? _maxEditorContainer;
	private NodeEditorProperty? _minEditor;
	private NodeEditorProperty? _maxEditor;
	private CheckBox? _inclusiveMaxCheckBox;
	private StatescriptVariableType _valueType = StatescriptVariableType.Int;
	private bool _isMaxInclusive = true;
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
		_isMaxInclusive = existing?.IsMaxInclusive ?? true;
		_factories = ResolverEditorFactoryCatalog.GetCompatibleFactories(GetBroadExpectedClrType(_valueType));

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		bool allowTypeSelection = expectedType == typeof(ForgeVariant128);
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
			_typeDropdown.Selected = GetTypeDropdownIndex(_valueType);
			_typeDropdown.ItemSelected += OnTypeDropdownItemSelected;
			typeRow.AddChild(_typeDropdown);
		}

		BuildResolverSlot(
			root,
			ResolverSlot.Min,
			"Min:",
			existing?.Left,
			existing?.MinFolded ?? true,
			out _minFoldable,
			out _minResolverDropdown,
			out _minEditorContainer,
			x => _minEditor = x);

		BuildResolverSlot(
			root,
			ResolverSlot.Max,
			"Max:",
			existing?.Right,
			existing?.MaxFolded ?? true,
			out _maxFoldable,
			out _maxResolverDropdown,
			out _maxEditorContainer,
			x => _maxEditor = x);

		_inclusiveMaxFoldable = new FoldableContainer
		{
			Title = "Inclusive Max:",
			Folded = existing?.InclusiveMaxFolded ?? true,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		_inclusiveMaxFoldable.FoldingChanged += OnInclusiveMaxFoldableFoldingChanged;
		root.AddChild(_inclusiveMaxFoldable);

		var inclusiveMaxRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_inclusiveMaxFoldable.AddChild(inclusiveMaxRow);

		var exclusiveButton = new CheckBox
		{
			Text = "Exclusive",
			ButtonPressed = !_isMaxInclusive,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		_inclusiveMaxCheckBox = new CheckBox
		{
			Text = "Inclusive",
			ButtonPressed = _isMaxInclusive,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		var buttonGroup = new ButtonGroup();
		exclusiveButton.ButtonGroup = buttonGroup;
		_inclusiveMaxCheckBox.ButtonGroup = buttonGroup;

		inclusiveMaxRow.AddChild(exclusiveButton);
		inclusiveMaxRow.AddChild(_inclusiveMaxCheckBox);

		exclusiveButton.Pressed += OnExclusiveMaxPressed;
		_inclusiveMaxCheckBox.Pressed += OnInclusiveMaxPressed;

		UpdateFoldableTitles();
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new RandomResolverResource
		{
			ValueType = _valueType,
			Left = SaveNestedEditor(_minEditor),
			MinFolded = _minFoldable?.Folded ?? false,
			Right = SaveNestedEditor(_maxEditor),
			MaxFolded = _maxFoldable?.Folded ?? false,
			IsMaxInclusive = _isMaxInclusive,
			InclusiveMaxFolded = _inclusiveMaxFoldable?.Folded ?? false,
		};
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_minEditor?.ClearCallbacks();
		_maxEditor?.ClearCallbacks();
		_typeDropdown = null;
		_minFoldable = null;
		_maxFoldable = null;
		_inclusiveMaxFoldable = null;
		_minResolverDropdown = null;
		_maxResolverDropdown = null;
		_minEditorContainer = null;
		_maxEditorContainer = null;
		_minEditor = null;
		_maxEditor = null;
		_inclusiveMaxCheckBox = null;
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
			StatescriptVariableType.Float => typeof(double),
			StatescriptVariableType.Double => typeof(double),
			_ => typeof(int),
		};
	}

	private static StatescriptVariableType GetDefaultValueType(Type expectedType)
	{
		if (expectedType == typeof(double))
		{
			return StatescriptVariableType.Double;
		}

		if (expectedType == typeof(float))
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
			StatescriptVariableType.Double => 1,
			_ => 0,
		};
	}

	private void BuildResolverSlot(
		VBoxContainer root,
		ResolverSlot slot,
		string title,
		StatescriptResolverResource? existingResolver,
		bool folded,
		out FoldableContainer foldable,
		out OptionButton resolverDropdown,
		out VBoxContainer editorContainer,
		Action<NodeEditorProperty?> setEditor)
	{
		foldable = new FoldableContainer { Title = title, Folded = folded };
		foldable.FoldingChanged += OnResolverSlotFoldableFoldingChanged;
		root.AddChild(foldable);

		var container = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		foldable.AddChild(container);

		resolverDropdown = CreateResolverDropdown(existingResolver);
		editorContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		container.AddChild(NestedResolverEditorUtilities.CreateResolverSelectorRow(resolverDropdown));
		container.AddChild(editorContainer);
		ShowEditor(GetSelectedIndex(existingResolver), existingResolver, editorContainer, setEditor);

		if (slot == ResolverSlot.Min)
		{
			resolverDropdown.ItemSelected += OnMinResolverDropdownItemSelected;
		}
		else
		{
			resolverDropdown.ItemSelected += OnMaxResolverDropdownItemSelected;
		}
	}

	private void OnTypeDropdownItemSelected(long index)
	{
		_valueType = index switch
		{
			1 => StatescriptVariableType.Double,
			_ => StatescriptVariableType.Int,
		};

		_factories = ResolverEditorFactoryCatalog.GetCompatibleFactories(GetBroadExpectedClrType(_valueType));
		RebuildResolverSlot(_minEditorContainer, x => _minEditor = x);
		RebuildResolverSlot(_maxEditorContainer, x => _maxEditor = x);
		UpdateFoldableTitles();
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

	private void OnInclusiveMaxChanged(bool isInclusive)
	{
		if (_isMaxInclusive == isInclusive)
		{
			return;
		}

		_isMaxInclusive = isInclusive;
		UpdateInclusiveMaxFoldableTitle();
		_onChanged?.Invoke();
	}

	private void OnExclusiveMaxPressed()
	{
		OnInclusiveMaxChanged(false);
	}

	private void OnInclusiveMaxPressed()
	{
		OnInclusiveMaxChanged(true);
	}

	private void OnInclusiveMaxFoldableFoldingChanged(bool folded)
	{
		UpdateInclusiveMaxFoldableTitle();
		_onChanged?.Invoke();
		RaiseLayoutSizeChanged();
	}

	private void OnResolverSlotFoldableFoldingChanged(bool folded)
	{
		UpdateFoldableTitles();
		_onChanged?.Invoke();
		RaiseLayoutSizeChanged();
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
			dropdown.AddItem(StatescriptResolverRegistry.GetDisplayName(factory));
		}

		int selectedIndex = GetSelectedIndex(null);
		dropdown.Selected = selectedIndex;
		HandleResolverChanged(selectedIndex, editorContainer, setEditor);
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
		UpdateFoldableTitles();
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
			dropdown.AddItem(StatescriptResolverRegistry.GetDisplayName(factory));
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
		StatescriptNodeProperty? tempProperty =
			existingResolver is null ? null : new StatescriptNodeProperty { Resolver = existingResolver };
		editor.Setup(_graph, tempProperty, GetExpectedClrType(_valueType), OnNestedEditorChanged, false);
		editor.LayoutSizeChanged += RaiseLayoutSizeChanged;
		container.AddChild(editor);
		setEditor(editor);
	}

	private void OnNestedEditorChanged()
	{
		UpdateFoldableTitles();
		_onChanged?.Invoke();
	}

	private void UpdateFoldableTitles()
	{
		if (_minFoldable is not null)
		{
			InlineConstantSummaryFormatter.ApplyFoldableTitle("Min:", _minFoldable, _minEditor);
		}

		if (_maxFoldable is not null)
		{
			InlineConstantSummaryFormatter.ApplyFoldableTitle("Max:", _maxFoldable, _maxEditor);
		}

		UpdateInclusiveMaxFoldableTitle();
	}

	private void UpdateInclusiveMaxFoldableTitle()
	{
		if (_inclusiveMaxFoldable is null)
		{
			return;
		}

		InlineConstantSummaryFormatter.ApplyFoldableTitle(
			"Max:",
			_inclusiveMaxFoldable,
			_isMaxInclusive ? "Inclusive" : "Exclusive",
			InlineSummaryBadgeKind.Enum);
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
