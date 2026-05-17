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
			GetConstrainedExpectedTypes(GetLeftFactoryExpectedTypes(expectedType), expectedType));
		_rightFactories = ResolverEditorFactoryCatalog.GetCompatibleFactories(
			GetConstrainedExpectedTypes(GetRightFactoryExpectedTypes(expectedType), expectedType));

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

		_leftFoldable = CreateFoldable(LeftTitle, existingResource?.LeftFolded ?? true);
		vBox.AddChild(_leftFoldable);
		var leftContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_leftFoldable.AddChild(leftContainer);
		_leftEditorContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_leftResolverDropdown = CreateResolverDropdownControl(_leftFactories, existingResource?.Left);
		leftContainer.AddChild(NestedResolverEditorUtilities.CreateResolverSelectorRow(_leftResolverDropdown));
		leftContainer.AddChild(_leftEditorContainer);

		ShowNestedEditor(
			_leftFactories,
			GetSelectedIndex(_leftFactories, existingResource?.Left),
			existingResource?.Left,
			GetConstrainedExpectedTypes(GetLeftFactoryExpectedTypes(expectedType), expectedType),
			_leftEditorContainer,
			x => _leftEditor = x);

		_leftResolverDropdown.ItemSelected += OnLeftResolverDropdownItemSelected;

		_rightFoldable = CreateFoldable(RightTitle, existingResource?.RightFolded ?? true);
		vBox.AddChild(_rightFoldable);
		var rightContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_rightFoldable.AddChild(rightContainer);
		_rightEditorContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_rightResolverDropdown = CreateResolverDropdownControl(_rightFactories, existingResource?.Right);
		rightContainer.AddChild(NestedResolverEditorUtilities.CreateResolverSelectorRow(_rightResolverDropdown));
		rightContainer.AddChild(_rightEditorContainer);

		ShowNestedEditor(
			_rightFactories,
			GetSelectedIndex(_rightFactories, existingResource?.Right),
			existingResource?.Right,
			GetConstrainedExpectedTypes(GetRightFactoryExpectedTypes(expectedType), expectedType),
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

	protected virtual Type[] GetLeftFactoryExpectedTypes(Type expectedType)
	{
		return LeftFactoryExpectedTypes;
	}

	protected virtual Type[] GetRightFactoryExpectedTypes(Type expectedType)
	{
		return RightFactoryExpectedTypes;
	}

	private static int GetSelectedIndex(
		List<Func<NodeEditorProperty>> factories,
		StatescriptResolverResource? existingResolver)
	{
		return NestedResolverEditorUtilities.GetSelectedIndex(factories, existingResolver);
	}

	private static OptionButton CreateResolverDropdownControl(
		List<Func<NodeEditorProperty>> factories,
		StatescriptResolverResource? existingResolver)
	{
		return NestedResolverEditorUtilities.CreateResolverDropdownControl(factories, existingResolver);
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
		HandleResolverDropdownChanged(
			_leftFactories,
			(int)index,
			GetConstrainedExpectedTypes(GetLeftFactoryExpectedTypes(_expectedType), _expectedType),
			_leftEditorContainer,
			x => _leftEditor = x);
	}

	private void OnRightResolverDropdownItemSelected(long index)
	{
		HandleResolverDropdownChanged(
			_rightFactories,
			(int)index,
			GetConstrainedExpectedTypes(GetRightFactoryExpectedTypes(_expectedType), _expectedType),
			_rightEditorContainer,
			x => _rightEditor = x);
	}

	private void HandleResolverDropdownChanged(
		List<Func<NodeEditorProperty>> factories,
		int selectedIndex,
		Type[] allowedExpectedTypes,
		VBoxContainer? editorContainer,
		Action<NodeEditorProperty?> setEditor)
	{
		if (editorContainer is null)
		{
			return;
		}

		NestedResolverEditorUtilities.ClearContainer(editorContainer);

		setEditor(null);
		ShowNestedEditor(
			factories,
			selectedIndex,
			null,
			allowedExpectedTypes,
			editorContainer,
			setEditor);
		UpdateFoldableTitles();
		_onChanged?.Invoke();
		RaiseLayoutSizeChanged();
	}

	private void ShowNestedEditor(
		List<Func<NodeEditorProperty>> factories,
		int factoryIndex,
		StatescriptResolverResource? existingResolver,
		Type[] allowedExpectedTypes,
		VBoxContainer? container,
		Action<NodeEditorProperty?> setEditor)
	{
		if (_graph is null || container is null || factoryIndex < 0 || factoryIndex >= factories.Count)
		{
			return;
		}

		NodeEditorProperty? editor = NestedResolverEditorUtilities.CreateNestedEditor(
			_graph,
			factories,
			factoryIndex,
			existingResolver,
			allowedExpectedTypes,
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
			InlineConstantSummaryFormatter.ApplyFoldableTitle(LeftTitle, _leftFoldable, _leftEditor);
		}

		if (_rightFoldable is not null)
		{
			InlineConstantSummaryFormatter.ApplyFoldableTitle(RightTitle, _rightFoldable, _rightEditor);
		}
	}

	private Type[] GetConstrainedExpectedTypes(Type[] candidateExpectedTypes, Type expectedType)
	{
		return NestedResolverEditorUtilities.ConstrainExpectedTypes(
			GetAllowedExpectedTypes(expectedType),
			candidateExpectedTypes);
	}
}
#endif
