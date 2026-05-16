// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class EntityVariableResolverEditor : NodeEditorProperty
{
	private const float LabelWidth = 62.0f;

	private readonly List<string> _graphVariableNames = [];
	private readonly List<string> _setPaths = [];
	private readonly List<string> _sharedVariableNames = [];

	private string _selectedVariableName = string.Empty;
	private string _selectedSetPath = string.Empty;
	private bool _sharedScope;
	private Action? _onChanged;

	private OptionButton? _scopeDropdown;
	private OptionButton? _graphVariableDropdown;
	private OptionButton? _setDropdown;
	private OptionButton? _sharedVariableDropdown;
	private Control? _graphRow;
	private Control? _setRow;
	private Control? _sharedVariableRow;

	public override string DisplayName => "Entity Variable";

	public override string ResolverTypeId => "EntityVariable";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(IForgeEntity);
	}

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		_onChanged = onChanged;

		if (property?.Resolver is EntityVariableResolverResource existing)
		{
			_sharedScope = existing.Scope == VariableScope.Shared;
			_selectedVariableName = existing.VariableName;
			_selectedSetPath = existing.SharedVariableSetPath;
		}

		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		_scopeDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_scopeDropdown.AddItem("Graph");
		_scopeDropdown.AddItem("Shared");
		_scopeDropdown.Selected = _sharedScope ? 1 : 0;
		_scopeDropdown.ItemSelected += OnScopeChanged;
		root.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow("Scope:", _scopeDropdown, LabelWidth));

		_graphVariableDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_graphVariableDropdown.SetMeta("is_variable_dropdown", true);
		_graphVariableDropdown.ItemSelected += OnGraphVariableChanged;
		_graphRow = ResolverEditorLayoutUtilities.CreateLabeledRow("Var:", _graphVariableDropdown, LabelWidth);
		root.AddChild(_graphRow);

		_setDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_setDropdown.ItemSelected += OnSetChanged;
		_setRow = ResolverEditorLayoutUtilities.CreateLabeledRow("Set:", _setDropdown, LabelWidth);
		root.AddChild(_setRow);

		_sharedVariableDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_sharedVariableDropdown.SetMeta("is_shared_variable_dropdown", true);
		_sharedVariableDropdown.ItemSelected += OnSharedVariableChanged;
		_sharedVariableRow = ResolverEditorLayoutUtilities.CreateLabeledRow(
			"Var:",
			_sharedVariableDropdown,
			LabelWidth);
		root.AddChild(_sharedVariableRow);

		PopulateGraphVariableDropdown(graph);
		PopulateSetDropdown();
		PopulateSharedVariableDropdown();
		UpdateRowVisibility();
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new EntityVariableResolverResource
		{
			VariableName = _selectedVariableName,
			Scope = _sharedScope ? VariableScope.Shared : VariableScope.Graph,
			SharedVariableSetPath = _selectedSetPath,
		};
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = string.IsNullOrWhiteSpace(_selectedVariableName) ? "(None)" : _selectedVariableName;
		return true;
	}

	public override InlineSummaryBadgeKind GetInlineSummaryBadgeKind()
	{
		return _sharedScope ? InlineSummaryBadgeKind.SharedVariable : InlineSummaryBadgeKind.Variable;
	}

	public override bool TryGetHighlightedVariableName(out string variableName)
	{
		variableName = _sharedScope ? string.Empty : _selectedVariableName;
		return !_sharedScope && !string.IsNullOrWhiteSpace(variableName);
	}

	public override bool TryGetHighlightedSharedVariable(out string sharedVariableSetPath, out string variableName)
	{
		sharedVariableSetPath = _sharedScope ? _selectedSetPath : string.Empty;
		variableName = _sharedScope ? _selectedVariableName : string.Empty;
		return _sharedScope
			&& !string.IsNullOrWhiteSpace(sharedVariableSetPath)
			&& !string.IsNullOrWhiteSpace(variableName);
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
	}

	private void OnScopeChanged(long index)
	{
		_sharedScope = index == 1;
		_selectedVariableName = string.Empty;
		UpdateRowVisibility();
		_onChanged?.Invoke();
	}

	private void OnGraphVariableChanged(long index)
	{
		int selectedIndex = (int)index;
		_selectedVariableName = selectedIndex >= 0 && selectedIndex < _graphVariableNames.Count
			? _graphVariableNames[selectedIndex]
			: string.Empty;
		_onChanged?.Invoke();
	}

	private void OnSetChanged(long index)
	{
		int selectedIndex = (int)index;
		_selectedSetPath = selectedIndex >= 0
			&& selectedIndex < _setPaths.Count
				? _setPaths[selectedIndex]
				: string.Empty;
		_selectedVariableName = string.Empty;
		PopulateSharedVariableDropdown();
		_onChanged?.Invoke();
	}

	private void OnSharedVariableChanged(long index)
	{
		int selectedIndex = (int)index;
		_selectedVariableName = selectedIndex >= 0 && selectedIndex < _sharedVariableNames.Count
			? _sharedVariableNames[selectedIndex]
			: string.Empty;
		_onChanged?.Invoke();
	}

	private void PopulateGraphVariableDropdown(StatescriptGraph graph)
	{
		if (_graphVariableDropdown is null)
		{
			return;
		}

		_graphVariableDropdown.Clear();
		_graphVariableNames.Clear();
		_graphVariableDropdown.AddItem("(None)");
		_graphVariableNames.Add(string.Empty);

		foreach (StatescriptGraphVariable variable in graph.Variables)
		{
			if (!variable.IsArray
				&& !string.IsNullOrEmpty(variable.VariableName)
				&& variable.VariableType == StatescriptVariableType.Entity)
			{
				_graphVariableDropdown.AddItem(variable.VariableName);
				_graphVariableNames.Add(variable.VariableName);
			}
		}

		ResolverEditorLayoutUtilities.RestoreSelection(
			_graphVariableDropdown,
			_graphVariableNames,
			_selectedVariableName);
	}

	private void PopulateSetDropdown()
	{
		if (_setDropdown is null)
		{
			return;
		}

		_setDropdown.Clear();
		_setPaths.Clear();
		_setDropdown.AddItem("(None)");
		_setPaths.Add(string.Empty);

		foreach (string path in VariableResolverEditorUtilities.FindAllSharedVariableSetPaths())
		{
			_setDropdown.AddItem(VariableResolverEditorUtilities.GetResourceDisplayName(path));
			_setPaths.Add(path);
		}

		ResolverEditorLayoutUtilities.RestoreSelection(_setDropdown, _setPaths, _selectedSetPath);
	}

	private void PopulateSharedVariableDropdown()
	{
		if (_sharedVariableDropdown is null)
		{
			return;
		}

		_sharedVariableDropdown.SetMeta("shared_variable_set_path", _selectedSetPath);
		_sharedVariableDropdown.Clear();
		_sharedVariableNames.Clear();
		_sharedVariableDropdown.AddItem("(None)");
		_sharedVariableNames.Add(string.Empty);

		if (!string.IsNullOrEmpty(_selectedSetPath) && ResourceLoader.Exists(_selectedSetPath))
		{
			ForgeSharedVariableSet? set = ResourceLoader.Load<ForgeSharedVariableSet>(_selectedSetPath);
			if (set is not null)
			{
				foreach (ForgeSharedVariableDefinition definition in set.Variables)
				{
					if (!definition.IsArray
						&& !string.IsNullOrEmpty(definition.VariableName)
						&& definition.VariableType == StatescriptVariableType.Entity)
					{
						_sharedVariableDropdown.AddItem(definition.VariableName);
						_sharedVariableNames.Add(definition.VariableName);
					}
				}
			}
		}

		ResolverEditorLayoutUtilities.RestoreSelection(
			_sharedVariableDropdown,
			_sharedVariableNames,
			_selectedVariableName);
	}

	private void UpdateRowVisibility()
	{
		if (_graphRow is not null)
		{
			_graphRow.Visible = !_sharedScope;
		}

		if (_setRow is not null)
		{
			_setRow.Visible = _sharedScope;
		}

		if (_sharedVariableRow is not null)
		{
			_sharedVariableRow.Visible = _sharedScope;
		}
	}
}
#endif
