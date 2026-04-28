// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;

internal abstract partial class AsymmetricBinaryNestedResolverEditorBase<TResource> : NodeEditorProperty
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
	private List<Func<NodeEditorProperty>> _leftFactories = [];
	private List<Func<NodeEditorProperty>> _rightFactories = [];

	protected abstract Type[] LeftFactoryExpectedTypes { get; }

	protected abstract Type[] RightFactoryExpectedTypes { get; }

	protected abstract Type LeftNestedExpectedType { get; }

	protected abstract Type RightNestedExpectedType { get; }

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
		_leftFactories = ResolverEditorFactoryCatalog.GetCompatibleFactories(
			GetLeftFactoryExpectedTypes(expectedType));
		_rightFactories = ResolverEditorFactoryCatalog.GetCompatibleFactories(
			GetRightFactoryExpectedTypes(expectedType));

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var vBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(vBox);

		if (_leftFactories.Count == 0 || _rightFactories.Count == 0)
		{
			var errorLabel = new Label { Text = "No compatible resolvers." };
			errorLabel.AddThemeColorOverride("font_color", Colors.Red);
			vBox.AddChild(errorLabel);
			return;
		}

		var existingResource = property?.Resolver as TResource;

		_leftFoldable = CreateFoldable(LeftTitle);
		vBox.AddChild(_leftFoldable);
		var leftContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_leftFoldable.AddChild(leftContainer);
		_leftEditorContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_leftResolverDropdown = CreateResolverDropdownControl(_leftFactories, existingResource?.Left);
		leftContainer.AddChild(_leftResolverDropdown);
		leftContainer.AddChild(_leftEditorContainer);

		ShowNestedEditor(
			_leftFactories,
			GetSelectedIndex(_leftFactories, existingResource?.Left),
			existingResource?.Left,
			GetLeftNestedExpectedType(expectedType),
			_leftEditorContainer,
			x => _leftEditor = x);

		_leftResolverDropdown.ItemSelected += OnLeftResolverDropdownItemSelected;

		_rightFoldable = CreateFoldable(RightTitle);
		vBox.AddChild(_rightFoldable);
		var rightContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_rightFoldable.AddChild(rightContainer);
		_rightEditorContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_rightResolverDropdown = CreateResolverDropdownControl(_rightFactories, existingResource?.Right);
		rightContainer.AddChild(_rightResolverDropdown);
		rightContainer.AddChild(_rightEditorContainer);

		ShowNestedEditor(
			_rightFactories,
			GetSelectedIndex(_rightFactories, existingResource?.Right),
			existingResource?.Right,
			GetRightNestedExpectedType(expectedType),
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
			Right = right,
		};
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_leftEditor?.ClearCallbacks();
		_rightEditor?.ClearCallbacks();
	}

	protected virtual Type[] GetLeftFactoryExpectedTypes(Type expectedType)
	{
		return LeftFactoryExpectedTypes;
	}

	protected virtual Type[] GetRightFactoryExpectedTypes(Type expectedType)
	{
		return RightFactoryExpectedTypes;
	}

	protected virtual Type GetLeftNestedExpectedType(Type expectedType)
	{
		return LeftNestedExpectedType;
	}

	protected virtual Type GetRightNestedExpectedType(Type expectedType)
	{
		return RightNestedExpectedType;
	}

	private static int GetSelectedIndex(
		List<Func<NodeEditorProperty>> factories,
		StatescriptResolverResource? existingResolver)
	{
		return ResolverEditorFactoryCatalog.GetDefaultFactoryIndex(factories, existingResolver, "Variant");
	}

	private static OptionButton CreateResolverDropdownControl(
		List<Func<NodeEditorProperty>> factories,
		StatescriptResolverResource? existingResolver)
	{
		var dropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };

		foreach (Func<NodeEditorProperty> factory in factories)
		{
			using NodeEditorProperty temp = factory();
			dropdown.AddItem(temp.DisplayName);
		}

		dropdown.Selected = GetSelectedIndex(factories, existingResolver);
		return dropdown;
	}

	private FoldableContainer CreateFoldable(string title)
	{
		var foldable = new FoldableContainer { Title = title };
		foldable.FoldingChanged += OnFoldingChanged;
		return foldable;
	}

	private void OnFoldingChanged(bool isFolded)
	{
		UpdateFoldableTitles();
		RaiseLayoutSizeChanged();
	}

	private void OnLeftResolverDropdownItemSelected(long index)
	{
		HandleResolverDropdownChanged(
			_leftFactories,
			(int)index,
			GetLeftNestedExpectedType(_expectedType),
			_leftEditorContainer,
			x => _leftEditor = x);
	}

	private void OnRightResolverDropdownItemSelected(long index)
	{
		HandleResolverDropdownChanged(
			_rightFactories,
			(int)index,
			GetRightNestedExpectedType(_expectedType),
			_rightEditorContainer,
			x => _rightEditor = x);
	}

	private void HandleResolverDropdownChanged(
		List<Func<NodeEditorProperty>> factories,
		int selectedIndex,
		Type nestedExpectedType,
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
		ShowNestedEditor(factories, selectedIndex, null, nestedExpectedType, editorContainer, setEditor);
		UpdateFoldableTitles();
		_onChanged?.Invoke();
		RaiseLayoutSizeChanged();
	}

	private void ShowNestedEditor(
		List<Func<NodeEditorProperty>> factories,
		int factoryIndex,
		StatescriptResolverResource? existingResolver,
		Type nestedExpectedType,
		VBoxContainer? container,
		Action<NodeEditorProperty?> setEditor)
	{
		if (_graph is null || container is null || factoryIndex < 0 || factoryIndex >= factories.Count)
		{
			return;
		}

		NodeEditorProperty editor = factories[factoryIndex]();
		StatescriptNodeProperty? tempProperty = existingResolver is null
			? null
			: new StatescriptNodeProperty { Resolver = existingResolver };

		editor.Setup(_graph, tempProperty, nestedExpectedType, OnNestedEditorChanged, false);
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
