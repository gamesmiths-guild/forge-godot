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

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that compares two nested numeric resolvers and produces a boolean result. Supports nesting any
/// numeric-compatible resolver as left/right operands, enabling powerful comparisons like "Attribute &gt; Constant".
/// </summary>
[Tool]
internal sealed partial class ComparisonResolverEditor : NodeEditorProperty
{
	private static readonly Type[] _numericExpectedTypes = [typeof(int), typeof(float), typeof(double)];
	private static readonly string[] _operationSymbols = ["==", "!=", "<", "<=", ">", ">="];

	private StatescriptGraph? _graph;
	private Action? _onChanged;

	private OptionButton? _operationDropdown;
	private VBoxContainer? _leftContainer;
	private VBoxContainer? _rightContainer;
	private FoldableContainer? _leftFoldable;
	private FoldableContainer? _rightFoldable;
	private FoldableContainer? _operationFoldable;
	private OptionButton? _leftResolverDropdown;
	private OptionButton? _rightResolverDropdown;

	private NodeEditorProperty? _leftEditor;
	private NodeEditorProperty? _rightEditor;

	private List<Func<NodeEditorProperty>> _numericFactories = [];
	private ComparisonOperation _operation;

	private VBoxContainer? _leftEditorContainer;
	private VBoxContainer? _rightEditorContainer;

	/// <inheritdoc/>
	public override string DisplayName => "Comparison";

	/// <inheritdoc/>
	public override string ResolverTypeId => "Comparison";

	/// <inheritdoc/>
	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(bool) || expectedType == typeof(ForgeVariant128);
	}

	/// <inheritdoc/>
	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		_graph = graph;
		_onChanged = onChanged;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var vBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(vBox);

		_numericFactories = ResolverEditorFactoryCatalog.GetCompatibleFactories(_numericExpectedTypes);

		_numericFactories.RemoveAll(x => StatescriptResolverRegistry.GetResolverTypeId(x) == ResolverTypeId);

		var comparisonResolver = property?.Resolver as ComparisonResolverResource;

		if (comparisonResolver is not null)
		{
			_operation = comparisonResolver.Operation;
		}

		_leftFoldable = InlineConstantSummaryFormatter.BuildColumnedFoldable(
			vBox,
			"Left:",
			comparisonResolver?.LeftFolded ?? true);
		_leftFoldable.FoldingChanged += OnFoldingChanged;

		_leftContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_leftFoldable.AddChild(_leftContainer);

		_leftEditorContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_leftResolverDropdown = CreateResolverDropdownControl(comparisonResolver?.Left);
		_leftContainer.AddChild(NestedResolverEditorUtilities.CreateResolverSelectorRow(_leftResolverDropdown));
		_leftContainer.AddChild(_leftEditorContainer);
		ShowNestedEditor(
			GetSelectedIndex(comparisonResolver?.Left),
			comparisonResolver?.Left,
			_leftEditorContainer,
			x => _leftEditor = x);

		_leftResolverDropdown.ItemSelected += OnLeftResolverDropdownItemSelected;

		_operationFoldable = InlineConstantSummaryFormatter.BuildColumnedFoldable(
			vBox,
			"Operator:",
			comparisonResolver?.OperationFolded ?? true);
		_operationFoldable.FoldingChanged += OnOperationFoldableFoldingChanged;

		var opContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_operationFoldable.AddChild(opContainer);

		_operationDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };

		foreach (string symbol in _operationSymbols)
		{
			_operationDropdown.AddItem(symbol);
		}

		_operationDropdown.Selected = (int)_operation;
		_operationDropdown.ItemSelected += OnOperationDropdownItemSelected;
		opContainer.AddChild(_operationDropdown);

		_rightFoldable = InlineConstantSummaryFormatter.BuildColumnedFoldable(
			vBox,
			"Right:",
			comparisonResolver?.RightFolded ?? true);
		_rightFoldable.FoldingChanged += OnFoldingChanged;

		_rightContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_rightFoldable.AddChild(_rightContainer);

		_rightEditorContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_rightResolverDropdown = CreateResolverDropdownControl(comparisonResolver?.Right);
		_rightContainer.AddChild(NestedResolverEditorUtilities.CreateResolverSelectorRow(_rightResolverDropdown));
		_rightContainer.AddChild(_rightEditorContainer);
		ShowNestedEditor(
			GetSelectedIndex(comparisonResolver?.Right),
			comparisonResolver?.Right,
			_rightEditorContainer,
			x => _rightEditor = x);

		_rightResolverDropdown.ItemSelected += OnRightResolverDropdownItemSelected;
		UpdateFoldableTitles();
	}

	/// <inheritdoc/>
	public override void SaveTo(StatescriptNodeProperty property)
	{
		var comparisonResolver = new ComparisonResolverResource
		{
			Operation = _operation,
			LeftFolded = _leftFoldable?.Folded ?? false,
			OperationFolded = _operationFoldable?.Folded ?? false,
			RightFolded = _rightFoldable?.Folded ?? false,
		};

		if (_leftEditor is not null)
		{
			var leftProperty = new StatescriptNodeProperty();
			_leftEditor.SaveTo(leftProperty);
			comparisonResolver.Left = leftProperty.Resolver;
		}

		if (_rightEditor is not null)
		{
			var rightProperty = new StatescriptNodeProperty();
			_rightEditor.SaveTo(rightProperty);
			comparisonResolver.Right = rightProperty.Resolver;
		}

		property.Resolver = comparisonResolver;
	}

	/// <inheritdoc/>
	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;

		_leftEditor?.ClearCallbacks();
		_rightEditor?.ClearCallbacks();
		_operationDropdown = null;
		_leftContainer = null;
		_rightContainer = null;
		_leftFoldable = null;
		_rightFoldable = null;
		_operationFoldable = null;
		_leftResolverDropdown = null;
		_rightResolverDropdown = null;
		_leftEditor = null;
		_rightEditor = null;
		_leftEditorContainer = null;
		_rightEditorContainer = null;
	}

	private void OnFoldingChanged(bool isFolded)
	{
		UpdateFoldableTitles();
		_onChanged?.Invoke();
		RaiseLayoutSizeChanged();
	}

	private void OnOperationFoldableFoldingChanged(bool isFolded)
	{
		UpdateOperationFoldableTitle();
		_onChanged?.Invoke();
		RaiseLayoutSizeChanged();
	}

	private void OnOperationDropdownItemSelected(long x)
	{
		_operation = (ComparisonOperation)(int)x;
		UpdateOperationFoldableTitle();
		_onChanged?.Invoke();
	}

	private void OnLeftResolverDropdownItemSelected(long x)
	{
		HandleResolverDropdownChanged((int)x, _leftEditorContainer, editor => _leftEditor = editor);
	}

	private void OnRightResolverDropdownItemSelected(long x)
	{
		HandleResolverDropdownChanged((int)x, _rightEditorContainer, editor => _rightEditor = editor);
	}

	private void HandleResolverDropdownChanged(
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
		ShowNestedEditor(selectedIndex, null, editorContainer, setEditor);
		UpdateFoldableTitles();
		_onChanged?.Invoke();
		RaiseLayoutSizeChanged();
	}

	private int GetSelectedIndex(StatescriptResolverResource? existingResolver)
	{
		return ResolverEditorFactoryCatalog.GetDefaultFactoryIndex(_numericFactories, existingResolver, "Variant");
	}

	private OptionButton CreateResolverDropdownControl(StatescriptResolverResource? existingResolver)
	{
		OptionButton dropdown = new SearchableOptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };

		foreach (Func<NodeEditorProperty> factory in _numericFactories)
		{
			dropdown.AddItem(StatescriptResolverRegistry.GetDisplayName(factory));
		}

		dropdown.Selected = GetSelectedIndex(existingResolver);

		return dropdown;
	}

	private void ShowNestedEditor(
		int factoryIndex,
		StatescriptResolverResource? existingResolver,
		VBoxContainer container,
		Action<NodeEditorProperty?> setEditor)
	{
		if (_graph is null || factoryIndex < 0 || factoryIndex >= _numericFactories.Count)
		{
			return;
		}

		NodeEditorProperty? editor = NestedResolverEditorUtilities.CreateNestedEditor(
			_graph,
			_numericFactories,
			factoryIndex,
			existingResolver,
			_numericExpectedTypes,
			OnNestedEditorChanged,
			RaiseLayoutSizeChanged);

		if (editor is null)
		{
			return;
		}

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
		if (_leftFoldable is not null)
		{
			InlineConstantSummaryFormatter.ApplyFoldableTitle("Left:", _leftFoldable, _leftEditor);
		}

		if (_rightFoldable is not null)
		{
			InlineConstantSummaryFormatter.ApplyFoldableTitle("Right:", _rightFoldable, _rightEditor);
		}

		UpdateOperationFoldableTitle();
	}

	private void UpdateOperationFoldableTitle()
	{
		if (_operationFoldable is not null)
		{
			InlineConstantSummaryFormatter.ApplyFoldableTitle(
				string.Empty,
				_operationFoldable,
				_operationSymbols[(int)_operation],
				InlineSummaryBadgeKind.Enum);
		}
	}
}
#endif
