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

	private StatescriptGraph? _graph;
	private Action? _onChanged;

	private OptionButton? _operationDropdown;
	private VBoxContainer? _leftContainer;
	private VBoxContainer? _rightContainer;
	private FoldableContainer? _leftFoldable;
	private FoldableContainer? _rightFoldable;
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

		_numericFactories.RemoveAll(x =>
		{
			using NodeEditorProperty temp = x();
			return temp.ResolverTypeId == "Comparison";
		});

		var comparisonResolver = property?.Resolver as ComparisonResolverResource;

		if (comparisonResolver is not null)
		{
			_operation = comparisonResolver.Operation;
		}

		_leftFoldable = new FoldableContainer
		{
			Title = "Left:",
			Folded = comparisonResolver?.LeftFolded ?? true,
		};

		_leftFoldable.FoldingChanged += OnFoldingChanged;
		vBox.AddChild(_leftFoldable);

		_leftContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_leftFoldable.AddChild(_leftContainer);

		_leftEditorContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_leftResolverDropdown = CreateResolverDropdownControl(comparisonResolver?.Left);
		_leftContainer.AddChild(_leftResolverDropdown);
		_leftContainer.AddChild(_leftEditorContainer);
		ShowNestedEditor(
			GetSelectedIndex(comparisonResolver?.Left),
			comparisonResolver?.Left,
			_leftEditorContainer,
			x => _leftEditor = x);

		_leftResolverDropdown.ItemSelected += OnLeftResolverDropdownItemSelected;

		var opRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		vBox.AddChild(opRow);

		opRow.AddChild(new Label { Text = "Op:" });

		_operationDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };

		_operationDropdown.AddItem("==");
		_operationDropdown.AddItem("!=");
		_operationDropdown.AddItem("<");
		_operationDropdown.AddItem("<=");
		_operationDropdown.AddItem(">");
		_operationDropdown.AddItem(">=");

		_operationDropdown.Selected = (int)_operation;

		_operationDropdown.ItemSelected += OnOperationDropdownItemSelected;

		opRow.AddChild(_operationDropdown);

		_rightFoldable = new FoldableContainer
		{
			Title = "Right:",
			Folded = comparisonResolver?.RightFolded ?? true,
		};
		_rightFoldable.FoldingChanged += OnFoldingChanged;
		vBox.AddChild(_rightFoldable);

		_rightContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_rightFoldable.AddChild(_rightContainer);

		_rightEditorContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_rightResolverDropdown = CreateResolverDropdownControl(comparisonResolver?.Right);
		_rightContainer.AddChild(_rightResolverDropdown);
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

	private void OnOperationDropdownItemSelected(long x)
	{
		_operation = (ComparisonOperation)(int)x;
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
		var dropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };

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
	}
}
#endif
