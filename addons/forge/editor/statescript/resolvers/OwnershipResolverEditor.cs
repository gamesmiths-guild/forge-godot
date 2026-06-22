// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class OwnershipResolverEditor : NodeEditorProperty
{
	private static readonly Type[] _entityExpectedTypes = [typeof(IForgeEntity)];

	private StatescriptGraph? _graph;
	private Action? _onChanged;
	private List<Func<NodeEditorProperty>> _entityFactories = [];

	private FoldableContainer? _ownerFoldable;
	private FoldableContainer? _sourceFoldable;
	private OptionButton? _ownerResolverDropdown;
	private OptionButton? _sourceResolverDropdown;
	private VBoxContainer? _ownerEditorContainer;
	private VBoxContainer? _sourceEditorContainer;
	private NodeEditorProperty? _ownerEditor;
	private NodeEditorProperty? _sourceEditor;

	public override string DisplayName => "Ownership";

	public override string ResolverTypeId => "Ownership";

	public override bool SupportsArrayValues => false;

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(EffectOwnership);
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
		_entityFactories = StatescriptResolverRegistry.GetCompatibleFactories(typeof(IForgeEntity));
		_entityFactories.RemoveAll(factory => !StatescriptResolverRegistry.SupportsScalarValues(factory));

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		var ownershipResolver = property?.Resolver as OwnershipResolverResource;

		_ownerFoldable = InlineConstantSummaryFormatter.BuildColumnedFoldable(
			root,
			"Owner:",
			ownershipResolver?.OwnerFolded ?? true);

		_ownerFoldable.FoldingChanged += OnFoldableChanged;

		var ownerContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_ownerFoldable.AddChild(ownerContainer);
		_ownerEditorContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_ownerResolverDropdown = CreateResolverDropdownControl(ownershipResolver?.Owner, "AbilityOwner");
		ownerContainer.AddChild(NestedResolverEditorUtilities.CreateResolverSelectorRow(_ownerResolverDropdown));
		ownerContainer.AddChild(_ownerEditorContainer);
		ShowNestedEditor(
			GetSelectedIndex(ownershipResolver?.Owner, "AbilityOwner"),
			ownershipResolver?.Owner,
			_ownerEditorContainer,
			editor => _ownerEditor = editor);
		_ownerResolverDropdown.ItemSelected += OnOwnerResolverDropdownItemSelected;

		_sourceFoldable = InlineConstantSummaryFormatter.BuildColumnedFoldable(
			root,
			"Source:",
			ownershipResolver?.SourceFolded ?? true);

		_sourceFoldable.FoldingChanged += OnFoldableChanged;

		var sourceContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_sourceFoldable.AddChild(sourceContainer);
		_sourceEditorContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_sourceResolverDropdown = CreateResolverDropdownControl(ownershipResolver?.Source, "AbilitySource");
		sourceContainer.AddChild(NestedResolverEditorUtilities.CreateResolverSelectorRow(_sourceResolverDropdown));
		sourceContainer.AddChild(_sourceEditorContainer);
		ShowNestedEditor(
			GetSelectedIndex(ownershipResolver?.Source, "AbilitySource"),
			ownershipResolver?.Source,
			_sourceEditorContainer,
			editor => _sourceEditor = editor);
		_sourceResolverDropdown.ItemSelected += OnSourceResolverDropdownItemSelected;

		UpdateFoldableTitles();
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		var resolver = new OwnershipResolverResource
		{
			OwnerFolded = _ownerFoldable?.Folded ?? false,
			SourceFolded = _sourceFoldable?.Folded ?? false,
		};

		if (_ownerEditor is not null)
		{
			var ownerProperty = new StatescriptNodeProperty();
			_ownerEditor.SaveTo(ownerProperty);
			resolver.Owner = ownerProperty.Resolver as EntityResolverResourceBase;
		}

		if (_sourceEditor is not null)
		{
			var sourceProperty = new StatescriptNodeProperty();
			_sourceEditor.SaveTo(sourceProperty);
			resolver.Source = sourceProperty.Resolver as EntityResolverResourceBase;
		}

		property.Resolver = resolver;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_ownerEditor?.ClearCallbacks();
		_sourceEditor?.ClearCallbacks();
		_ownerFoldable = null;
		_sourceFoldable = null;
		_ownerResolverDropdown = null;
		_sourceResolverDropdown = null;
		_ownerEditorContainer = null;
		_sourceEditorContainer = null;
		_ownerEditor = null;
		_sourceEditor = null;
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = $"{GetNestedSummary(_ownerEditor, "Owner")} / {GetNestedSummary(_sourceEditor, "Source")}";
		return true;
	}

	private static string GetNestedSummary(NodeEditorProperty? editor, string fallback)
	{
		return editor is not null && editor.TryGetInlineSummary(out string summary) ? summary : fallback;
	}

	private void OnFoldableChanged(bool isFolded)
	{
		UpdateFoldableTitles();
		_onChanged?.Invoke();
		RaiseLayoutSizeChanged();
	}

	private void OnOwnerResolverDropdownItemSelected(long index)
	{
		HandleResolverDropdownChanged((int)index, _ownerEditorContainer, editor => _ownerEditor = editor);
	}

	private void OnSourceResolverDropdownItemSelected(long index)
	{
		HandleResolverDropdownChanged((int)index, _sourceEditorContainer, editor => _sourceEditor = editor);
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

		NestedResolverEditorUtilities.ClearContainer(editorContainer);
		setEditor(null);
		ShowNestedEditor(selectedIndex, null, editorContainer, setEditor);
		UpdateFoldableTitles();
		_onChanged?.Invoke();
		RaiseLayoutSizeChanged();
	}

	private OptionButton CreateResolverDropdownControl(
		EntityResolverResourceBase? existingResolver,
		string preferredResolverTypeId)
	{
		var dropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };

		foreach (Func<NodeEditorProperty> factory in _entityFactories)
		{
			dropdown.AddItem(StatescriptResolverRegistry.GetDisplayName(factory));
		}

		dropdown.Selected = GetSelectedIndex(existingResolver, preferredResolverTypeId);
		return dropdown;
	}

	private int GetSelectedIndex(EntityResolverResourceBase? existingResolver, string preferredResolverTypeId)
	{
		if (existingResolver is not null)
		{
			for (int i = 0; i < _entityFactories.Count; i++)
			{
				if (StatescriptResolverRegistry.GetResolverTypeId(_entityFactories[i]) == existingResolver.ResolverTypeId)
				{
					return i;
				}
			}
		}

		for (int i = 0; i < _entityFactories.Count; i++)
		{
			if (StatescriptResolverRegistry.GetResolverTypeId(_entityFactories[i]) == preferredResolverTypeId)
			{
				return i;
			}
		}

		return 0;
	}

	private void ShowNestedEditor(
		int factoryIndex,
		EntityResolverResourceBase? existingResolver,
		VBoxContainer container,
		Action<NodeEditorProperty?> setEditor)
	{
		NodeEditorProperty? editor = NestedResolverEditorUtilities.CreateNestedEditor(
			_graph,
			_entityFactories,
			factoryIndex,
			existingResolver,
			_entityExpectedTypes,
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
		if (_ownerFoldable is not null)
		{
			InlineConstantSummaryFormatter.ApplyFoldableTitle("Owner:", _ownerFoldable, _ownerEditor);
		}

		if (_sourceFoldable is not null)
		{
			InlineConstantSummaryFormatter.ApplyFoldableTitle("Source:", _sourceFoldable, _sourceEditor);
		}
	}
}
#endif
