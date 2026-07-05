// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;

/// <summary>
/// Reusable control that authors a nested resolver inside a titled foldable: a searchable dropdown of compatible
/// resolver editors plus the selected editor's UI. Supports both scalar operands (predicates, keys, counts) and array
/// operands (the source of an array operation).
/// </summary>
[Tool]
internal sealed partial class NestedResolverPicker : VBoxContainer
{
	/// <summary>
	/// Safety net against cyclic default selections: when pickers auto-instantiate their default nested editor during
	/// <see cref="Initialize"/>, the build depth is tracked and capped so a resolver editor whose default operand is
	/// another recursive editor can never freeze the editor process.
	/// </summary>
	private const int MaxAutomaticBuildDepth = 4;

	private static int _automaticBuildDepth;

	private StatescriptGraph? _graph;
	private Action? _onChanged;
	private Action? _layoutSizeChanged;
	private FoldableContainer? _foldable;
	private VBoxContainer? _editorContainer;
	private NodeEditorProperty? _editor;
	private List<Func<NodeEditorProperty>> _factories = [];
	private Type[] _expectedTypes = [];
	private bool _isArray;
	private bool _iterationScope;
	private string _title = string.Empty;

	public bool Folded => _foldable?.Folded ?? true;

	public bool HasCompatibleFactories => _factories.Count > 0;

	public void Initialize(
		StatescriptGraph graph,
		StatescriptResolverResource? existingResolver,
		string title,
		Type[] expectedTypes,
		bool isArray,
		bool folded,
		Action onChanged,
		Action layoutSizeChanged,
		bool iterationScope = false)
	{
		_graph = graph;
		_onChanged = onChanged;
		_layoutSizeChanged = layoutSizeChanged;
		_expectedTypes = expectedTypes;
		_isArray = isArray;
		_iterationScope = iterationScope;
		_title = title;

		_factories = isArray
			? ResolverEditorFactoryCatalog.GetArrayCompatibleFactories(iterationScope, expectedTypes)
			: ResolverEditorFactoryCatalog.GetCompatibleFactories(iterationScope, expectedTypes);

		SizeFlagsHorizontal = SizeFlags.ExpandFill;

		_foldable = InlineConstantSummaryFormatter.BuildColumnedFoldable(title, folded);
		_foldable.FoldingChanged += OnFoldingChanged;
		AddChild(_foldable);

		var content = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_foldable.AddChild(content);

		if (_factories.Count == 0)
		{
			var errorLabel = new Label { Text = "No compatible resolvers." };
			errorLabel.AddThemeColorOverride("font_color", Colors.Red);
			content.AddChild(errorLabel);
			return;
		}

		OptionButton dropdown = NestedResolverEditorUtilities.CreateResolverDropdownControl(
			_factories,
			existingResolver);
		int selectedIndex = GetInitialFactoryIndex(existingResolver);
		dropdown.Selected = selectedIndex;
		content.AddChild(NestedResolverEditorUtilities.CreateResolverSelectorRow(dropdown, isArray));

		_editorContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		content.AddChild(_editorContainer);

		ShowNestedEditor(selectedIndex, existingResolver);
		dropdown.ItemSelected += OnDropdownItemSelected;
		UpdateFoldableTitle();

		// The picker is built before it enters the scene tree, so the title pill is measured against stale sizes.
		// Defer one layout pass (the same thing a fold or toggle interaction triggers) so it settles correctly.
		Callable.From(() => _layoutSizeChanged?.Invoke()).CallDeferred();
	}

	public StatescriptResolverResource? BuildResource()
	{
		if (_editor is null)
		{
			return null;
		}

		var property = new StatescriptNodeProperty();
		_editor.SaveTo(property);
		return property.Resolver;
	}

	public void ClearCallbacks()
	{
		_onChanged = null;
		_layoutSizeChanged = null;
		_editor?.ClearCallbacks();
	}

	public bool TryGetHighlightedVariableName(out string variableName)
	{
		if (_editor is not null && _editor.TryGetHighlightedVariableName(out variableName))
		{
			return true;
		}

		variableName = string.Empty;
		return false;
	}

	public bool TryGetHighlightedSharedVariable(out string sharedVariableSetPath, out string variableName)
	{
		if (_editor is not null
			&& _editor.TryGetHighlightedSharedVariable(out sharedVariableSetPath, out variableName))
		{
			return true;
		}

		sharedVariableSetPath = string.Empty;
		variableName = string.Empty;
		return false;
	}

	private static bool TryBeginAutomaticBuild()
	{
		if (_automaticBuildDepth >= MaxAutomaticBuildDepth)
		{
			return false;
		}

		_automaticBuildDepth++;
		return true;
	}

	private static void EndAutomaticBuild()
	{
		_automaticBuildDepth--;
	}

	private void OnFoldingChanged(bool isFolded)
	{
		UpdateFoldableTitle();
		_onChanged?.Invoke();
		_layoutSizeChanged?.Invoke();
	}

	private void OnDropdownItemSelected(long index)
	{
		if (_editorContainer is null)
		{
			return;
		}

		NestedResolverEditorUtilities.ClearContainer(_editorContainer);
		_editor = null;
		ShowNestedEditor((int)index, null);
		UpdateFoldableTitle();
		_onChanged?.Invoke();
		_layoutSizeChanged?.Invoke();
	}

	/// <summary>
	/// Picks the initial dropdown selection: the existing resolver's editor when restoring, otherwise a leaf resolver
	/// (Variable for arrays, Constant for scalars) so the auto-instantiated default never recurses into another
	/// operation editor.
	/// </summary>
	/// <param name="existingResolver">The existing resolver to restore, or null to auto-instantiate a default.</param>
	private int GetInitialFactoryIndex(StatescriptResolverResource? existingResolver)
	{
		if (existingResolver is not null)
		{
			for (int i = 0; i < _factories.Count; i++)
			{
				if (StatescriptResolverRegistry.GetResolverTypeId(_factories[i]) == existingResolver.ResolverTypeId)
				{
					return i;
				}
			}
		}

		string[] types;
		if (_isArray)
		{
			types = ["Variable", "Variant"];
		}
		else
		{
			types = ["Variant", "Variable"];
		}

		foreach (string preferredTypeId in types)
		{
			for (int i = 0; i < _factories.Count; i++)
			{
				if (StatescriptResolverRegistry.GetResolverTypeId(_factories[i]) == preferredTypeId)
				{
					return i;
				}
			}
		}

		return 0;
	}

	private void ShowNestedEditor(int factoryIndex, StatescriptResolverResource? existingResolver)
	{
		if (_graph is null || _editorContainer is null)
		{
			return;
		}

		if (!TryBeginAutomaticBuild())
		{
			_editorContainer.AddChild(new Label { Text = "Max nesting depth reached." });
			return;
		}

		try
		{
			NodeEditorProperty? editor = NestedResolverEditorUtilities.CreateNestedEditor(
				_graph,
				_factories,
				factoryIndex,
				existingResolver,
				_expectedTypes,
				OnNestedEditorChanged,
				() => _layoutSizeChanged?.Invoke(),
				_isArray,
				_iterationScope);

			if (editor is null)
			{
				return;
			}

			_editorContainer.AddChild(editor);
			_editor = editor;
		}
		finally
		{
			EndAutomaticBuild();
		}
	}

	private void OnNestedEditorChanged()
	{
		UpdateFoldableTitle();
		_onChanged?.Invoke();
	}

	private void UpdateFoldableTitle()
	{
		if (_foldable is not null)
		{
			InlineConstantSummaryFormatter.ApplyFoldableTitle(_title, _foldable, _editor);
		}
	}
}
#endif
