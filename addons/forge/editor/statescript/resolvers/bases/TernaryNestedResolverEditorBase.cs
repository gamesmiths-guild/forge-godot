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
	private enum ResolverSlot
	{
		First = 0,
		Second = 1,
		Third = 2,
	}

	private StatescriptGraph? _graph;
	private Action? _onChanged;
	private FoldableContainer? _firstFoldable;
	private FoldableContainer? _secondFoldable;
	private FoldableContainer? _thirdFoldable;
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
			ResolverEditorFactoryCatalog.GetCompatibleFactories(
				GetConstrainedExpectedTypes(GetFirstFactoryExpectedTypes(expectedType), expectedType));
		_secondFactories =
			ResolverEditorFactoryCatalog.GetCompatibleFactories(
				GetConstrainedExpectedTypes(GetSecondFactoryExpectedTypes(expectedType), expectedType));
		_thirdFactories =
			ResolverEditorFactoryCatalog.GetCompatibleFactories(
				GetConstrainedExpectedTypes(GetThirdFactoryExpectedTypes(expectedType), expectedType));

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
			ResolverSlot.First,
			FirstTitle,
			_firstFactories,
			existingResource?.First,
			GetConstrainedExpectedTypes(GetFirstFactoryExpectedTypes(expectedType), expectedType),
			existingResource?.FirstFolded ?? true);

		BuildSlot(
			vBox,
			ResolverSlot.Second,
			SecondTitle,
			_secondFactories,
			existingResource?.Second,
			GetConstrainedExpectedTypes(GetSecondFactoryExpectedTypes(expectedType), expectedType),
			existingResource?.SecondFolded ?? true);

		BuildSlot(
			vBox,
			ResolverSlot.Third,
			ThirdTitle,
			_thirdFactories,
			existingResource?.Third,
			GetConstrainedExpectedTypes(GetThirdFactoryExpectedTypes(expectedType), expectedType),
			existingResource?.ThirdFolded ?? true);

		UpdateFoldableTitles();
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new TResource
		{
			First = SaveNestedEditor(_firstEditor),
			FirstFolded = _firstFoldable?.Folded ?? false,
			Second = SaveNestedEditor(_secondEditor),
			SecondFolded = _secondFoldable?.Folded ?? false,
			Third = SaveNestedEditor(_thirdEditor),
			ThirdFolded = _thirdFoldable?.Folded ?? false,
		};
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_firstEditor?.ClearCallbacks();
		_secondEditor?.ClearCallbacks();
		_thirdEditor?.ClearCallbacks();
		_firstFoldable = null;
		_secondFoldable = null;
		_thirdFoldable = null;
		_firstEditor = null;
		_secondEditor = null;
		_thirdEditor = null;
	}

	public override bool TryGetHighlightedVariableName(out string variableName)
	{
		if (_firstEditor is not null && _firstEditor.TryGetHighlightedVariableName(out variableName))
		{
			return true;
		}

		if (_secondEditor is not null && _secondEditor.TryGetHighlightedVariableName(out variableName))
		{
			return true;
		}

		if (_thirdEditor is not null && _thirdEditor.TryGetHighlightedVariableName(out variableName))
		{
			return true;
		}

		variableName = string.Empty;
		return false;
	}

	public override bool TryGetHighlightedSharedVariable(out string sharedVariableSetPath, out string variableName)
	{
		if (_firstEditor is not null
			&& _firstEditor.TryGetHighlightedSharedVariable(out sharedVariableSetPath, out variableName))
		{
			return true;
		}

		if (_secondEditor is not null
			&& _secondEditor.TryGetHighlightedSharedVariable(out sharedVariableSetPath, out variableName))
		{
			return true;
		}

		if (_thirdEditor is not null
			&& _thirdEditor.TryGetHighlightedSharedVariable(out sharedVariableSetPath, out variableName))
		{
			return true;
		}

		sharedVariableSetPath = string.Empty;
		variableName = string.Empty;
		return false;
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
		return NestedResolverEditorUtilities.GetSelectedIndex(factories, existingResolver);
	}

	private static VBoxContainer? GetNestedEditorContainer(FoldableContainer? foldable)
	{
		if (foldable is null || foldable.GetChildCount() <= 0)
		{
			return null;
		}

		if (foldable.GetChild(0) is not VBoxContainer slotContainer || slotContainer.GetChildCount() <= 1)
		{
			return null;
		}

		return slotContainer.GetChild(1) as VBoxContainer;
	}

	private void BuildSlot(
		VBoxContainer root,
		ResolverSlot slot,
		string title,
		List<Func<NodeEditorProperty>> factories,
		StatescriptResolverResource? existingResolver,
		Type[] allowedExpectedTypes,
		bool folded)
	{
		FoldableContainer foldable = new() { Title = title, Folded = folded };
		foldable.FoldingChanged += OnFoldableFoldingChanged;
		SetFoldable(slot, foldable);
		root.AddChild(foldable);

		var container = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		foldable.AddChild(container);

		OptionButton resolverDropdown =
			NestedResolverEditorUtilities.CreateResolverDropdownControl(factories, existingResolver);

		int selectedIndex = GetSelectedIndex(factories, existingResolver);
		resolverDropdown.Selected = selectedIndex;
		container.AddChild(resolverDropdown);

		var editorContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		container.AddChild(editorContainer);
		ShowNestedEditor(
			slot,
			factories,
			selectedIndex,
			existingResolver,
			allowedExpectedTypes,
			editorContainer);

		resolverDropdown.ItemSelected += slot switch
		{
			ResolverSlot.First => OnFirstResolverDropdownItemSelected,
			ResolverSlot.Second => OnSecondResolverDropdownItemSelected,
			_ => OnThirdResolverDropdownItemSelected,
		};
	}

	private void ShowNestedEditor(
		ResolverSlot slot,
		List<Func<NodeEditorProperty>> factories,
		int factoryIndex,
		StatescriptResolverResource? existingResolver,
		Type[] allowedExpectedTypes,
		VBoxContainer container)
	{
		if (_graph is null || factoryIndex < 0 || factoryIndex >= factories.Count)
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
		SetEditor(slot, editor);
	}

	private void OnFoldableFoldingChanged(bool folded)
	{
		UpdateFoldableTitles();
		_onChanged?.Invoke();
		RaiseLayoutSizeChanged();
	}

	private void OnFirstResolverDropdownItemSelected(long index)
	{
		HandleResolverDropdownChanged(ResolverSlot.First, (int)index, _firstFactories, GetFirstAllowedExpectedTypes());
	}

	private void OnSecondResolverDropdownItemSelected(long index)
	{
		HandleResolverDropdownChanged(ResolverSlot.Second, (int)index, _secondFactories, GetSecondAllowedExpectedTypes());
	}

	private void OnThirdResolverDropdownItemSelected(long index)
	{
		HandleResolverDropdownChanged(ResolverSlot.Third, (int)index, _thirdFactories, GetThirdAllowedExpectedTypes());
	}

	private void HandleResolverDropdownChanged(
		ResolverSlot slot,
		int index,
		List<Func<NodeEditorProperty>> factories,
		Type[] allowedExpectedTypes)
	{
		VBoxContainer? editorContainer = GetEditorContainer(slot);
		if (editorContainer is null)
		{
			return;
		}

		NestedResolverEditorUtilities.ClearContainer(editorContainer);
		SetEditor(slot, null);
		ShowNestedEditor(slot, factories, index, null, allowedExpectedTypes, editorContainer);
		UpdateFoldableTitles();
		_onChanged?.Invoke();
		RaiseLayoutSizeChanged();
	}

	private Type[] GetFirstAllowedExpectedTypes()
	{
		return GetConstrainedExpectedTypes(FirstFactoryExpectedTypes, FirstNestedExpectedType);
	}

	private Type[] GetSecondAllowedExpectedTypes()
	{
		return GetConstrainedExpectedTypes(SecondFactoryExpectedTypes, SecondNestedExpectedType);
	}

	private Type[] GetThirdAllowedExpectedTypes()
	{
		return GetConstrainedExpectedTypes(ThirdFactoryExpectedTypes, ThirdNestedExpectedType);
	}

	private void SetFoldable(ResolverSlot slot, FoldableContainer foldable)
	{
		switch (slot)
		{
			case ResolverSlot.First:
				_firstFoldable = foldable;
				break;
			case ResolverSlot.Second:
				_secondFoldable = foldable;
				break;
			default:
				_thirdFoldable = foldable;
				break;
		}
	}

	private void SetEditor(ResolverSlot slot, NodeEditorProperty? editor)
	{
		switch (slot)
		{
			case ResolverSlot.First:
				_firstEditor = editor;
				break;
			case ResolverSlot.Second:
				_secondEditor = editor;
				break;
			default:
				_thirdEditor = editor;
				break;
		}
	}

	private VBoxContainer? GetEditorContainer(ResolverSlot slot)
	{
		return slot switch
		{
			ResolverSlot.First => GetNestedEditorContainer(_firstFoldable),
			ResolverSlot.Second => GetNestedEditorContainer(_secondFoldable),
			_ => GetNestedEditorContainer(_thirdFoldable),
		};
	}

	private void OnNestedEditorChanged()
	{
		UpdateFoldableTitles();
		_onChanged?.Invoke();
	}

	private void UpdateFoldableTitles()
	{
		if (_firstFoldable is not null)
		{
			InlineConstantSummaryFormatter.ApplyFoldableTitle(
				FirstTitle,
				_firstFoldable,
				_firstEditor);
		}

		if (_secondFoldable is not null)
		{
			InlineConstantSummaryFormatter.ApplyFoldableTitle(
				SecondTitle,
				_secondFoldable,
				_secondEditor);
		}

		if (_thirdFoldable is not null)
		{
			InlineConstantSummaryFormatter.ApplyFoldableTitle(
				ThirdTitle,
				_thirdFoldable,
				_thirdEditor);
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
