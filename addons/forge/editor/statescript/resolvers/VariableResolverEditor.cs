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

/// <summary>
/// Resolver editor that binds an input property to a graph or shared variable. The editor filters available variables
/// by scope, type compatibility, and array-ness, and saves the selection to <see cref="VariableResolverResource"/>.
/// </summary>
[Tool]
internal sealed partial class VariableResolverEditor : NodeEditorProperty
{
	private const float LabelWidth = 62.0f;

	private readonly List<VariableOption> _graphVariables = [];
	private readonly List<string> _setPaths = [];
	private readonly List<VariableOption> _sharedVariables = [];

	private StatescriptGraph? _graph;
	private OptionButton? _scopeDropdown;
	private OptionButton? _graphVariableDropdown;
	private OptionButton? _setDropdown;
	private OptionButton? _sharedVariableDropdown;
	private Control? _graphRow;
	private Control? _setRow;
	private Control? _sharedVariableRow;
	private Action? _onChanged;

	private Type _expectedType = typeof(Variant128);
	private bool _expectedIsArray;
	private VariableScope _selectedScope;
	private string _selectedSetPath = string.Empty;
	private string _selectedVariableName = string.Empty;
	private StatescriptVariableType _selectedVariableType = StatescriptVariableType.Int;
	private bool _selectedIsArray;

	/// <inheritdoc/>
	public override string DisplayName => "Variable";

	/// <inheritdoc/>
	public override string ResolverTypeId => "Variable";

	/// <inheritdoc/>
	public override bool IsCompatibleWith(Type expectedType)
	{
		return true;
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
		_expectedType = expectedType;
		_expectedIsArray = isArray;

		LoadExistingSelection(property?.Resolver);

		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		_scopeDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_scopeDropdown.AddItem("Graph");
		_scopeDropdown.AddItem("Shared");
		_scopeDropdown.Selected = _selectedScope == VariableScope.Shared ? 1 : 0;
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

	/// <inheritdoc/>
	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new VariableResolverResource
		{
			VariableName = _selectedVariableName,
			Scope = _selectedScope,
			SharedVariableSetPath = _selectedScope == VariableScope.Shared ? _selectedSetPath : string.Empty,
			VariableType = _selectedVariableType,
			IsArray = _selectedIsArray,
		};
	}

	/// <inheritdoc/>
	public override bool TryGetInlineSummary(out string summary)
	{
		summary = string.IsNullOrWhiteSpace(_selectedVariableName)
			? "(None)"
			: _selectedVariableName;
		return true;
	}

	/// <inheritdoc/>
	public override InlineSummaryBadgeKind GetInlineSummaryBadgeKind()
	{
		return _selectedScope == VariableScope.Shared
			? InlineSummaryBadgeKind.SharedVariable
			: InlineSummaryBadgeKind.Variable;
	}

	/// <inheritdoc/>
	public override bool TryGetHighlightedVariableName(out string variableName)
	{
		variableName = _selectedScope == VariableScope.Graph ? _selectedVariableName : string.Empty;
		return !string.IsNullOrWhiteSpace(variableName);
	}

	/// <inheritdoc/>
	public override bool TryGetHighlightedSharedVariable(out string sharedVariableSetPath, out string variableName)
	{
		sharedVariableSetPath = _selectedScope == VariableScope.Shared ? _selectedSetPath : string.Empty;
		variableName = _selectedScope == VariableScope.Shared ? _selectedVariableName : string.Empty;
		return !string.IsNullOrWhiteSpace(sharedVariableSetPath)
			&& !string.IsNullOrWhiteSpace(variableName);
	}

	/// <inheritdoc/>
	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
	}

	private static bool IsCompatibleType(Type expectedType, StatescriptVariableType variableType)
	{
		return StatescriptVariableTypeConverter.IsCompatible(expectedType, variableType);
	}

	private static bool IsCompatibleType(Type[] expectedTypes, StatescriptVariableType variableType)
	{
		for (int i = 0; i < expectedTypes.Length; i++)
		{
			if (IsCompatibleType(expectedTypes[i], variableType))
			{
				return true;
			}
		}

		return false;
	}

	private static List<string> GetVariableNames(List<VariableOption> variables)
	{
		var names = new List<string>(variables.Count);
		for (int i = 0; i < variables.Count; i++)
		{
			names.Add(variables[i].Name);
		}

		return names;
	}

	private void LoadExistingSelection(StatescriptResolverResource? resolver)
	{
		if (resolver is VariableResolverResource variableResolver)
		{
			_selectedScope = variableResolver.Scope;
			_selectedSetPath = variableResolver.SharedVariableSetPath;
			_selectedVariableName = variableResolver.VariableName;
			_selectedVariableType = variableResolver.VariableType;
			_selectedIsArray = variableResolver.IsArray;
		}
	}

	private void OnScopeChanged(long index)
	{
		_selectedScope = index == 1 ? VariableScope.Shared : VariableScope.Graph;
		_selectedVariableName = string.Empty;
		ResetSelectedVariableMetadata();

		if (_graph is not null)
		{
			PopulateGraphVariableDropdown(_graph);
		}

		PopulateSharedVariableDropdown();
		UpdateRowVisibility();
		RaiseLayoutSizeChanged();
		_onChanged?.Invoke();
	}

	private void OnGraphVariableChanged(long index)
	{
		int selectedIndex = (int)index;

		if (selectedIndex >= 0 && selectedIndex < _graphVariables.Count)
		{
			ApplySelection(_graphVariables[selectedIndex]);
		}
		else
		{
			_selectedVariableName = string.Empty;
			ResetSelectedVariableMetadata();
		}

		_onChanged?.Invoke();
	}

	private void OnSetChanged(long index)
	{
		int selectedIndex = (int)index;
		_selectedSetPath = selectedIndex >= 0 && selectedIndex < _setPaths.Count
			? _setPaths[selectedIndex]
			: string.Empty;
		_selectedVariableName = string.Empty;
		ResetSelectedVariableMetadata();
		PopulateSharedVariableDropdown();
		_onChanged?.Invoke();
	}

	private void OnSharedVariableChanged(long index)
	{
		int selectedIndex = (int)index;

		if (selectedIndex >= 0 && selectedIndex < _sharedVariables.Count)
		{
			ApplySelection(_sharedVariables[selectedIndex]);
		}
		else
		{
			_selectedVariableName = string.Empty;
			ResetSelectedVariableMetadata();
		}

		_onChanged?.Invoke();
	}

	private void PopulateGraphVariableDropdown(StatescriptGraph graph)
	{
		if (_graphVariableDropdown is null)
		{
			return;
		}

		_graphVariableDropdown.Clear();
		_graphVariables.Clear();
		_graphVariableDropdown.AddItem("(None)");
		_graphVariables.Add(new VariableOption(string.Empty, StatescriptVariableType.Int, false));

		Type[] allowedExpectedTypes = GetAllowedExpectedTypes(_expectedType);

		foreach (StatescriptGraphVariable variable in graph.Variables)
		{
			if (string.IsNullOrEmpty(variable.VariableName)
				|| variable.IsArray != _expectedIsArray
				|| !IsCompatibleType(allowedExpectedTypes, variable.VariableType))
			{
				continue;
			}

			_graphVariableDropdown.AddItem(variable.VariableName);
			_graphVariables.Add(new VariableOption(variable.VariableName, variable.VariableType, variable.IsArray));
		}

		ResolverEditorLayoutUtilities.RestoreSelection(
			_graphVariableDropdown,
			GetVariableNames(_graphVariables),
			_selectedScope == VariableScope.Graph ? _selectedVariableName : string.Empty);

		if (_graphVariableDropdown.Selected == 0)
		{
			if (_selectedScope == VariableScope.Graph)
			{
				_selectedVariableName = string.Empty;
				ResetSelectedVariableMetadata();
			}

			return;
		}

		ApplySelection(_graphVariables[_graphVariableDropdown.Selected]);
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
		if (_setDropdown.Selected == 0)
		{
			_selectedSetPath = string.Empty;
		}
	}

	private void PopulateSharedVariableDropdown()
	{
		if (_sharedVariableDropdown is null)
		{
			return;
		}

		_sharedVariableDropdown.SetMeta("shared_variable_set_path", _selectedSetPath);
		_sharedVariableDropdown.Clear();
		_sharedVariables.Clear();
		_sharedVariableDropdown.AddItem("(None)");
		_sharedVariables.Add(new VariableOption(string.Empty, StatescriptVariableType.Int, false));

		Type[] allowedExpectedTypes = GetAllowedExpectedTypes(_expectedType);

		if (!string.IsNullOrEmpty(_selectedSetPath) && ResourceLoader.Exists(_selectedSetPath))
		{
			ForgeSharedVariableSet? set = ResourceLoader.Load<ForgeSharedVariableSet>(_selectedSetPath);

			if (set is not null)
			{
				foreach (ForgeSharedVariableDefinition definition in set.Variables)
				{
					if (string.IsNullOrEmpty(definition.VariableName)
						|| definition.IsArray != _expectedIsArray
						|| !IsCompatibleType(allowedExpectedTypes, definition.VariableType))
					{
						continue;
					}

					_sharedVariableDropdown.AddItem(definition.VariableName);
					_sharedVariables.Add(
						new VariableOption(definition.VariableName, definition.VariableType, definition.IsArray));
				}
			}
		}

		ResolverEditorLayoutUtilities.RestoreSelection(
			_sharedVariableDropdown,
			GetVariableNames(_sharedVariables),
			_selectedScope == VariableScope.Shared ? _selectedVariableName : string.Empty);

		if (_sharedVariableDropdown.Selected == 0)
		{
			if (_selectedScope == VariableScope.Shared)
			{
				_selectedVariableName = string.Empty;
				ResetSelectedVariableMetadata();
			}

			return;
		}

		ApplySelection(_sharedVariables[_sharedVariableDropdown.Selected]);
	}

	private void ApplySelection(VariableOption option)
	{
		_selectedVariableName = option.Name;
		_selectedVariableType = option.VariableType;
		_selectedIsArray = option.IsArray;
	}

	private void ResetSelectedVariableMetadata()
	{
		_selectedVariableType = _expectedType == typeof(IForgeEntity)
			? StatescriptVariableType.Entity
			: StatescriptVariableType.Int;
		_selectedIsArray = _expectedIsArray;
	}

	private void UpdateRowVisibility()
	{
		if (_graphRow is not null)
		{
			_graphRow.Visible = _selectedScope == VariableScope.Graph;
		}

		if (_setRow is not null)
		{
			_setRow.Visible = _selectedScope == VariableScope.Shared;
		}

		if (_sharedVariableRow is not null)
		{
			_sharedVariableRow.Visible = _selectedScope == VariableScope.Shared;
		}
	}

	private readonly record struct VariableOption(string Name, StatescriptVariableType VariableType, bool IsArray);
}
#endif
