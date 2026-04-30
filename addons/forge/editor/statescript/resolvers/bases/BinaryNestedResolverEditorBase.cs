// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;

internal abstract partial class BinaryNestedResolverEditorBase<TResource> : NodeEditorProperty
	where TResource : BinaryNestedResolverResourceBase, new()
{
	private StatescriptGraph? _graph;
	private Action? _onChanged;
	private Type _expectedType = typeof(ForgeVariant128);
	private OptionButton? _leftResolverDropdown;
	private OptionButton? _rightResolverDropdown;
	private FoldableContainer? _leftFoldable;
	private FoldableContainer? _rightFoldable;
	private VBoxContainer? _leftEditorContainer;
	private VBoxContainer? _rightEditorContainer;
	private NodeEditorProperty? _leftEditor;
	private NodeEditorProperty? _rightEditor;
	private List<Func<NodeEditorProperty>> _factories = [];

	protected abstract Type[] FactoryExpectedTypes { get; }

	protected abstract Type NestedExpectedType { get; }

	protected virtual string LeftTitle => "Left:";

	protected virtual string RightTitle => "Right:";

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

		_leftFoldable = CreateFoldable(LeftTitle, existingResource?.LeftFolded ?? true);
		vBox.AddChild(_leftFoldable);
		var leftContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_leftFoldable.AddChild(leftContainer);
		_leftEditorContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_leftResolverDropdown = CreateResolverDropdownControl(existingResource?.Left);
		leftContainer.AddChild(_leftResolverDropdown);
		leftContainer.AddChild(_leftEditorContainer);

		ShowNestedEditor(
			GetSelectedIndex(existingResource?.Left),
			existingResource?.Left,
			_leftEditorContainer,
			x => _leftEditor = x);

		_leftResolverDropdown.ItemSelected += OnLeftResolverDropdownItemSelected;

		_rightFoldable = CreateFoldable(RightTitle, existingResource?.RightFolded ?? true);
		vBox.AddChild(_rightFoldable);
		var rightContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_rightFoldable.AddChild(rightContainer);
		_rightEditorContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_rightResolverDropdown = CreateResolverDropdownControl(existingResource?.Right);
		rightContainer.AddChild(_rightResolverDropdown);
		rightContainer.AddChild(_rightEditorContainer);

		ShowNestedEditor(
			GetSelectedIndex(existingResource?.Right),
			existingResource?.Right,
			_rightEditorContainer,
			x => _rightEditor = x);

		_rightResolverDropdown.ItemSelected += OnRightResolverDropdownItemSelected;
		UpdateFoldableTitles();
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		StatescriptResolverResource? left = null;
		StatescriptResolverResource? right = null;

		if (_leftEditor is not null)
		{
			var leftProperty = new StatescriptNodeProperty();
			_leftEditor.SaveTo(leftProperty);
			left = leftProperty.Resolver;
		}

		if (_rightEditor is not null)
		{
			var rightProperty = new StatescriptNodeProperty();
			_rightEditor.SaveTo(rightProperty);
			right = rightProperty.Resolver;
		}

		property.Resolver = new TResource
		{
			Left = left,
			LeftFolded = _leftFoldable?.Folded ?? false,
			Right = right,
			RightFolded = _rightFoldable?.Folded ?? false,
		};
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_leftEditor?.ClearCallbacks();
		_rightEditor?.ClearCallbacks();
	}

	public override bool TryGetHighlightedVariableName(out string variableName)
	{
		if (_leftEditor is not null && _leftEditor.TryGetHighlightedVariableName(out variableName))
		{
			return true;
		}

		if (_rightEditor is not null && _rightEditor.TryGetHighlightedVariableName(out variableName))
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
		UpdateFoldableTitles();
		_onChanged?.Invoke();
		RaiseLayoutSizeChanged();
	}

	private void OnLeftResolverDropdownItemSelected(long index)
	{
		HandleResolverDropdownChanged((int)index, _leftEditorContainer, x => _leftEditor = x);
	}

	private void OnRightResolverDropdownItemSelected(long index)
	{
		HandleResolverDropdownChanged((int)index, _rightEditorContainer, x => _rightEditor = x);
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

	private void ShowNestedEditor(
		int factoryIndex,
		StatescriptResolverResource? existingResolver,
		VBoxContainer? container,
		Action<NodeEditorProperty?> setEditor)
	{
		if (_graph is null || container is null || factoryIndex < 0 || factoryIndex >= _factories.Count)
		{
			return;
		}

		NodeEditorProperty editor = _factories[factoryIndex]();
		StatescriptNodeProperty? tempProperty = existingResolver is null
			? null
			: new StatescriptNodeProperty { Resolver = existingResolver };

		editor.Setup(_graph, tempProperty, GetNestedExpectedType(_expectedType), OnNestedEditorChanged, false);
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
		if (_leftFoldable is not null)
		{
			InlineConstantSummaryFormatter.ApplyFoldableTitle(LeftTitle, _leftFoldable, _leftEditor);
		}

		if (_rightFoldable is not null)
		{
			InlineConstantSummaryFormatter.ApplyFoldableTitle(RightTitle, _rightFoldable, _rightEditor);
		}
	}
}
#endif
