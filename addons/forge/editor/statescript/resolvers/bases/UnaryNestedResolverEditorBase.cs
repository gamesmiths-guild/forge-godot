// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;

internal abstract partial class UnaryNestedResolverEditorBase<TResource> : NodeEditorProperty
	where TResource : UnaryNestedResolverResourceBase, new()
{
	private StatescriptGraph? _graph;
	private Action? _onChanged;
	private Type _expectedType = typeof(ForgeVariant128);
	private FoldableContainer? _operandFoldable;
	private OptionButton? _resolverDropdown;
	private VBoxContainer? _editorContainer;
	private NodeEditorProperty? _operandEditor;
	private List<Func<NodeEditorProperty>> _factories = [];

	protected abstract Type[] FactoryExpectedTypes { get; }

	protected abstract Type NestedExpectedType { get; }

	protected virtual string OperandTitle => "Operand:";

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
		_factories = ResolverEditorFactoryCatalog.GetCompatibleFactories(GetFactoryExpectedTypes(expectedType));

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var vBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(vBox);

		if (_factories.Count == 0)
		{
			var errorLabel = new Label { Text = "No compatible resolvers." };
			errorLabel.AddThemeColorOverride("font_color", Colors.Red);
			vBox.AddChild(errorLabel);
			return;
		}

		var existingResource = property?.Resolver as TResource;
		_operandFoldable = CreateFoldable(OperandTitle, existingResource?.OperandFolded ?? true);
		vBox.AddChild(_operandFoldable);

		var operandContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_operandFoldable.AddChild(operandContainer);

		_editorContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_resolverDropdown = CreateResolverDropdownControl(existingResource?.Operand);
		operandContainer.AddChild(_resolverDropdown);
		operandContainer.AddChild(_editorContainer);

		ShowNestedEditor(GetSelectedIndex(existingResource?.Operand), existingResource?.Operand);
		_resolverDropdown.ItemSelected += OnResolverDropdownItemSelected;
		UpdateFoldableTitle();
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

		property.Resolver = new TResource
		{
			Operand = operand,
			OperandFolded = _operandFoldable?.Folded ?? false,
		};
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_operandEditor?.ClearCallbacks();
	}

	public override bool TryGetHighlightedVariableName(out string variableName)
	{
		if (_operandEditor is not null && _operandEditor.TryGetHighlightedVariableName(out variableName))
		{
			return true;
		}

		variableName = string.Empty;
		return false;
	}

	protected virtual Type[] GetFactoryExpectedTypes(Type expectedType)
	{
		return FactoryExpectedTypes;
	}

	protected virtual Type GetNestedExpectedType(Type expectedType)
	{
		return NestedExpectedType;
	}

	private FoldableContainer CreateFoldable(string title, bool folded)
	{
		var foldable = new FoldableContainer { Title = title, Folded = folded };
		foldable.FoldingChanged += OnFoldingChanged;
		return foldable;
	}

	private void OnFoldingChanged(bool isFolded)
	{
		UpdateFoldableTitle();
		_onChanged?.Invoke();
		RaiseLayoutSizeChanged();
	}

	private void OnResolverDropdownItemSelected(long index)
	{
		HandleResolverDropdownChanged((int)index);
	}

	private void HandleResolverDropdownChanged(int selectedIndex)
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
		ShowNestedEditor(selectedIndex, null);
		UpdateFoldableTitle();
		_onChanged?.Invoke();
		RaiseLayoutSizeChanged();
	}

	private int GetSelectedIndex(StatescriptResolverResource? existingResolver)
	{
		return ResolverEditorFactoryCatalog.GetDefaultFactoryIndex(_factories, existingResolver, "Variant");
	}

	private OptionButton CreateResolverDropdownControl(StatescriptResolverResource? existingResolver)
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

	private void ShowNestedEditor(int factoryIndex, StatescriptResolverResource? existingResolver)
	{
		if (_graph is null || _editorContainer is null || factoryIndex < 0 || factoryIndex >= _factories.Count)
		{
			return;
		}

		NodeEditorProperty editor = _factories[factoryIndex]();
		StatescriptNodeProperty? tempProperty = existingResolver is null
			? null
			: new StatescriptNodeProperty { Resolver = existingResolver };

		editor.Setup(_graph, tempProperty, GetNestedExpectedType(_expectedType), OnNestedEditorChanged, false);
		editor.LayoutSizeChanged += RaiseLayoutSizeChanged;
		_editorContainer.AddChild(editor);
		_operandEditor = editor;
	}

	private void OnNestedEditorChanged()
	{
		UpdateFoldableTitle();
		_onChanged?.Invoke();
	}

	private void UpdateFoldableTitle()
	{
		if (_operandFoldable is not null)
		{
			InlineConstantSummaryFormatter.ApplyFoldableTitle(
				OperandTitle,
				_operandFoldable,
				_operandEditor);
		}
	}
}
#endif
