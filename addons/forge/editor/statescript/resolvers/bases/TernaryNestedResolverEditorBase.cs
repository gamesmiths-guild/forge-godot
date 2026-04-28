// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;

internal abstract partial class TernaryNestedResolverEditorBase<TResource> : NodeEditorProperty
	where TResource : TernaryNestedResolverResourceBase, new()
{
	private StatescriptGraph? _graph;
	private Action? _onChanged;
	private NodeEditorProperty? _firstEditor;
	private NodeEditorProperty? _secondEditor;
	private NodeEditorProperty? _thirdEditor;
	private List<Func<NodeEditorProperty>> _firstFactories = [];
	private List<Func<NodeEditorProperty>> _secondFactories = [];
	private List<Func<NodeEditorProperty>> _thirdFactories = [];

	protected abstract Type[] FirstFactoryExpectedTypes { get; }

	protected abstract Type[] SecondFactoryExpectedTypes { get; }

	protected abstract Type[] ThirdFactoryExpectedTypes { get; }

	protected abstract Type FirstNestedExpectedType { get; }

	protected abstract Type SecondNestedExpectedType { get; }

	protected abstract Type ThirdNestedExpectedType { get; }

	protected virtual string FirstTitle => "First:";

	protected virtual string SecondTitle => "Second:";

	protected virtual string ThirdTitle => "Third:";

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		_graph = graph;
		_onChanged = onChanged;
		_firstFactories =
			ResolverEditorFactoryCatalog.GetCompatibleFactories(GetFirstFactoryExpectedTypes(expectedType));
		_secondFactories =
			ResolverEditorFactoryCatalog.GetCompatibleFactories(GetSecondFactoryExpectedTypes(expectedType));
		_thirdFactories =
			ResolverEditorFactoryCatalog.GetCompatibleFactories(GetThirdFactoryExpectedTypes(expectedType));

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var vBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(vBox);

		if (_firstFactories.Count == 0 || _secondFactories.Count == 0 || _thirdFactories.Count == 0)
		{
			var errorLabel = new Label { Text = "No compatible resolvers." };
			errorLabel.AddThemeColorOverride("font_color", Colors.Red);
			vBox.AddChild(errorLabel);
			return;
		}

		var existingResource = property?.Resolver as TResource;

		BuildSlot(
			vBox,
			FirstTitle,
			_firstFactories,
			existingResource?.First,
			GetFirstNestedExpectedType(expectedType),
			x => _firstEditor = x);

		BuildSlot(
			vBox,
			SecondTitle,
			_secondFactories,
			existingResource?.Second,
			GetSecondNestedExpectedType(expectedType),
			x => _secondEditor = x);

		BuildSlot(
			vBox,
			ThirdTitle,
			_thirdFactories,
			existingResource?.Third,
			GetThirdNestedExpectedType(expectedType),
			x => _thirdEditor = x);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new TResource
		{
			First = SaveNestedEditor(_firstEditor),
			Second = SaveNestedEditor(_secondEditor),
			Third = SaveNestedEditor(_thirdEditor),
		};
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_firstEditor?.ClearCallbacks();
		_secondEditor?.ClearCallbacks();
		_thirdEditor?.ClearCallbacks();
	}

	protected virtual Type[] GetFirstFactoryExpectedTypes(Type expectedType)
	{
		return FirstFactoryExpectedTypes;
	}

	protected virtual Type[] GetSecondFactoryExpectedTypes(Type expectedType)
	{
		return SecondFactoryExpectedTypes;
	}

	protected virtual Type[] GetThirdFactoryExpectedTypes(Type expectedType)
	{
		return ThirdFactoryExpectedTypes;
	}

	protected virtual Type GetFirstNestedExpectedType(Type expectedType)
	{
		return FirstNestedExpectedType;
	}

	protected virtual Type GetSecondNestedExpectedType(Type expectedType)
	{
		return SecondNestedExpectedType;
	}

	protected virtual Type GetThirdNestedExpectedType(Type expectedType)
	{
		return ThirdNestedExpectedType;
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

	private static int GetSelectedIndex(
		List<Func<NodeEditorProperty>> factories,
		StatescriptResolverResource? existingResolver)
	{
		return ResolverEditorFactoryCatalog.GetDefaultFactoryIndex(factories, existingResolver, "Variant");
	}

	private void BuildSlot(
		VBoxContainer root,
		string title,
		List<Func<NodeEditorProperty>> factories,
		StatescriptResolverResource? existingResolver,
		Type nestedExpectedType,
		Action<NodeEditorProperty?> setEditor)
	{
		FoldableContainer foldable = new() { Title = title };
		foldable.FoldingChanged += _ => RaiseLayoutSizeChanged();
		root.AddChild(foldable);

		var container = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		foldable.AddChild(container);

		var resolverDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		foreach (Func<NodeEditorProperty> factory in factories)
		{
			using NodeEditorProperty temp = factory();
			resolverDropdown.AddItem(temp.DisplayName);
		}

		var selectedIndex = GetSelectedIndex(factories, existingResolver);
		resolverDropdown.Selected = selectedIndex;
		container.AddChild(resolverDropdown);

		var editorContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		container.AddChild(editorContainer);
		ShowNestedEditor(factories, selectedIndex, existingResolver, nestedExpectedType, editorContainer, setEditor);

		resolverDropdown.ItemSelected += index =>
		{
			foreach (Node child in editorContainer.GetChildren())
			{
				editorContainer.RemoveChild(child);
				child.Free();
			}

			setEditor(null);
			ShowNestedEditor(factories, (int)index, null, nestedExpectedType, editorContainer, setEditor);
			_onChanged?.Invoke();
			RaiseLayoutSizeChanged();
		};
	}

	private void ShowNestedEditor(
		List<Func<NodeEditorProperty>> factories,
		int factoryIndex,
		StatescriptResolverResource? existingResolver,
		Type nestedExpectedType,
		VBoxContainer container,
		Action<NodeEditorProperty?> setEditor)
	{
		if (_graph is null || factoryIndex < 0 || factoryIndex >= factories.Count)
		{
			return;
		}

		NodeEditorProperty editor = factories[factoryIndex]();
		StatescriptNodeProperty? tempProperty =
			existingResolver is null ? null : new StatescriptNodeProperty { Resolver = existingResolver };
		editor.Setup(_graph, tempProperty, nestedExpectedType, OnNestedEditorChanged, false);
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
