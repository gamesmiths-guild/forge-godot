// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
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
	private StatescriptGraph? _graph;
	private Action? _onChanged;

	private OptionButton? _operationDropdown;
	private VBoxContainer? _leftContainer;
	private VBoxContainer? _rightContainer;
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

		_numericFactories = StatescriptResolverRegistry.GetCompatibleFactories(typeof(ForgeVariant128));

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

		var leftFoldable = new FoldableContainer { Title = "Left:" };
		leftFoldable.FoldingChanged += OnFoldingChanged;
		vBox.AddChild(leftFoldable);

		_leftContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		leftFoldable.AddChild(_leftContainer);

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

		var rightFoldable = new FoldableContainer { Title = "Right:" };
		rightFoldable.FoldingChanged += OnFoldingChanged;
		vBox.AddChild(rightFoldable);

		_rightContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		rightFoldable.AddChild(_rightContainer);

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
	}

	/// <inheritdoc/>
	public override void SaveTo(StatescriptNodeProperty property)
	{
		var comparisonResolver = new ComparisonResolverResource { Operation = _operation };

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
	}

	private void OnFoldingChanged(bool isFolded)
	{
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
		_onChanged?.Invoke();
		RaiseLayoutSizeChanged();
	}

	private int GetSelectedIndex(StatescriptResolverResource? existingResolver)
	{
		var selectedIndex = 0;

		if (existingResolver is not null)
		{
			var existingTypeId = existingResolver.ResolverTypeId;

			for (var i = 0; i < _numericFactories.Count; i++)
			{
				using NodeEditorProperty temp = _numericFactories[i]();

				if (temp.ResolverTypeId == existingTypeId)
				{
					selectedIndex = i;
					break;
				}
			}
		}

		return selectedIndex;
	}

	private OptionButton CreateResolverDropdownControl(StatescriptResolverResource? existingResolver)
	{
		var dropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };

		foreach (Func<NodeEditorProperty> factory in _numericFactories)
		{
			using NodeEditorProperty temp = factory();
			dropdown.AddItem(temp.DisplayName);
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

		NodeEditorProperty editor = _numericFactories[factoryIndex]();

		StatescriptNodeProperty? tempProperty = null;

		if (existingResolver is not null)
		{
			tempProperty = new StatescriptNodeProperty { Resolver = existingResolver };
		}

		editor.Setup(_graph, tempProperty, typeof(ForgeVariant128), OnNestedEditorChanged, false);

		editor.LayoutSizeChanged += RaiseLayoutSizeChanged;

		container.AddChild(editor);
		setEditor(editor);
	}

	private void OnNestedEditorChanged()
	{
		_onChanged?.Invoke();
	}
}
#endif
