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
internal sealed partial class RoundResolverEditor : NodeEditorProperty
{
	private Action? _onChanged;
	private StatescriptGraph? _graph;
	private OptionButton? _operandResolverDropdown;
	private VBoxContainer? _operandEditorContainer;
	private NodeEditorProperty? _operandEditor;
	private List<Func<NodeEditorProperty>> _operandFactories = [];
	private SpinBox? _digitsSpin;
	private OptionButton? _modeDropdown;
	private int _digits;
	private MidpointRounding _mode = MidpointRounding.ToEven;

	public override string DisplayName => "Round";

	public override string ResolverTypeId => "Round";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(ForgeVariant128) || ResolverEditorCompatibility.IsFloatType(expectedType);
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
		_operandFactories = ResolverEditorFactoryCatalog.GetCompatibleFactories(typeof(float), typeof(double));
		var existing = property?.Resolver as RoundResolverResource;
		_digits = existing?.Digits ?? 0;
		_mode = existing?.Mode ?? MidpointRounding.ToEven;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		FoldableContainer operandFoldable = CreateFoldable("Operand:");
		root.AddChild(operandFoldable);
		var operandContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		operandFoldable.AddChild(operandContainer);
		_operandResolverDropdown = CreateResolverDropdown(existing?.Operand);
		_operandEditorContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		operandContainer.AddChild(_operandResolverDropdown);
		operandContainer.AddChild(_operandEditorContainer);
		ShowOperandEditor(GetSelectedIndex(existing?.Operand), existing?.Operand);
		_operandResolverDropdown.ItemSelected += OnOperandResolverDropdownItemSelected;

		var digitsRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		root.AddChild(digitsRow);
		digitsRow.AddChild(new Label
		{
			Text = "Digits:",
			CustomMinimumSize = new GodotVector2(55, 0),
			HorizontalAlignment = HorizontalAlignment.Right,
		});

		_digitsSpin = new SpinBox
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			MinValue = 0,
			MaxValue = 15,
			Step = 1,
			Rounded = true,
			Value = _digits,
		};
		_digitsSpin.ValueChanged += OnDigitsChanged;
		digitsRow.AddChild(_digitsSpin);

		var modeRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		root.AddChild(modeRow);
		modeRow.AddChild(new Label
		{
			Text = "Mode:",
			CustomMinimumSize = new GodotVector2(55, 0),
			HorizontalAlignment = HorizontalAlignment.Right,
		});

		_modeDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		foreach (MidpointRounding value in Enum.GetValues<MidpointRounding>())
		{
			_modeDropdown.AddItem(value.ToString());
		}

		_modeDropdown.Selected = (int)_mode;
		_modeDropdown.ItemSelected += OnModeDropdownItemSelected;
		modeRow.AddChild(_modeDropdown);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		StatescriptResolverResource? operand = null;
		if (_operandEditor is not null)
		{
			var operandProperty = new StatescriptNodeProperty();
			_operandEditor.SaveTo(operandProperty);
			operand = operandProperty.Resolver;
		}

		property.Resolver = new RoundResolverResource
		{
			Operand = operand,
			Digits = _digits,
			Mode = _mode,
		};
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_operandEditor?.ClearCallbacks();
	}

	private FoldableContainer CreateFoldable(string title)
	{
		var foldable = new FoldableContainer { Title = title };
		foldable.FoldingChanged += OnFoldingChanged;
		return foldable;
	}

	private void OnFoldingChanged(bool isFolded)
	{
		RaiseLayoutSizeChanged();
	}

	private void OnOperandResolverDropdownItemSelected(long index)
	{
		if (_operandEditorContainer is null)
		{
			return;
		}

		foreach (Node child in _operandEditorContainer.GetChildren())
		{
			_operandEditorContainer.RemoveChild(child);
			child.Free();
		}

		_operandEditor = null;
		ShowOperandEditor((int)index, null);
		_onChanged?.Invoke();
		RaiseLayoutSizeChanged();
	}

	private void OnDigitsChanged(double value)
	{
		_digits = (int)value;
		_onChanged?.Invoke();
	}

	private void OnModeDropdownItemSelected(long index)
	{
		_mode = (MidpointRounding)(int)index;
		_onChanged?.Invoke();
	}

	private int GetSelectedIndex(StatescriptResolverResource? existingResolver)
	{
		return ResolverEditorFactoryCatalog.GetDefaultFactoryIndex(_operandFactories, existingResolver, "Variant");
	}

	private OptionButton CreateResolverDropdown(StatescriptResolverResource? existingResolver)
	{
		var dropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		foreach (Func<NodeEditorProperty> factory in _operandFactories)
		{
			using NodeEditorProperty temp = factory();
			dropdown.AddItem(temp.DisplayName);
		}

		dropdown.Selected = GetSelectedIndex(existingResolver);
		return dropdown;
	}

	private void ShowOperandEditor(int factoryIndex, StatescriptResolverResource? existingResolver)
	{
		if (_graph is null || _operandEditorContainer is null || factoryIndex < 0 || factoryIndex >= _operandFactories.Count)
		{
			return;
		}

		NodeEditorProperty editor = _operandFactories[factoryIndex]();
		StatescriptNodeProperty? tempProperty = existingResolver is null ? null : new StatescriptNodeProperty { Resolver = existingResolver };
		editor.Setup(_graph, tempProperty, typeof(float), OnNestedEditorChanged, false);
		editor.LayoutSizeChanged += RaiseLayoutSizeChanged;
		_operandEditorContainer.AddChild(editor);
		_operandEditor = editor;
	}

	private void OnNestedEditorChanged()
	{
		_onChanged?.Invoke();
	}
}
#endif
