// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that binds an input property to a graph variable. Only variables whose type is compatible with the
/// expected type are shown in the dropdown.
/// </summary>
internal sealed class VariableResolverEditor : IStatescriptResolverEditor
{
	private readonly List<string> _variableNames = [];

	private OptionButton? _dropdown;
	private string _selectedVariableName = string.Empty;

	/// <inheritdoc/>
	public string DisplayName => "Variable";

	/// <inheritdoc/>
	public Type ValueType => typeof(Variant128);

	/// <inheritdoc/>
	public string ResolverTypeId => "Variable";

	/// <inheritdoc/>
	public bool IsCompatibleWith(Type expectedType)
	{
		// Variable resolver is compatible with anything since we filter by variable type at selection time.
		return true;
	}

	/// <inheritdoc/>
	public Control? CreateEditorUI(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged)
	{
		_dropdown = new OptionButton
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(100, 0),
		};

		PopulateDropdown(graph, expectedType);

		// Restore selection from existing binding.
		if (property?.Resolver is VariableResolverResource varRes
			&& !string.IsNullOrEmpty(varRes.VariableName))
		{
			_selectedVariableName = varRes.VariableName;
			SelectByName(varRes.VariableName);
		}

		_dropdown.ItemSelected += _ =>
		{
			var idx = _dropdown.Selected;
			_selectedVariableName = idx >= 0 && idx < _variableNames.Count ? _variableNames[idx] : string.Empty;
			onChanged();
		};

		return _dropdown;
	}

	/// <inheritdoc/>
	public void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new VariableResolverResource
		{
			VariableName = _selectedVariableName,
		};
	}

	/// <summary>
	/// Refreshes the dropdown list when graph variables change.
	/// </summary>
	/// <param name="graph">The current graph.</param>
	/// <param name="expectedType">The expected type for filtering.</param>
	public void RefreshDropdown(StatescriptGraph graph, Type expectedType)
	{
		if (_dropdown is null)
		{
			return;
		}

		var previousSelection = _selectedVariableName;
		PopulateDropdown(graph, expectedType);
		SelectByName(previousSelection);
	}

	private void PopulateDropdown(StatescriptGraph graph, Type expectedType)
	{
		if (_dropdown is null)
		{
			return;
		}

		_dropdown.Clear();
		_variableNames.Clear();

		_dropdown.AddItem("(None)");
		_variableNames.Add(string.Empty);

		foreach (StatescriptGraphVariable variable in graph.Variables)
		{
			if (string.IsNullOrEmpty(variable.VariableName))
			{
				continue;
			}

			if (!StatescriptVariableTypeConverter.IsCompatible(expectedType, variable.VariableType))
			{
				continue;
			}

			_dropdown.AddItem(variable.VariableName);
			_variableNames.Add(variable.VariableName);
		}
	}

	private void SelectByName(string name)
	{
		if (_dropdown is null || string.IsNullOrEmpty(name))
		{
			return;
		}

		for (var i = 0; i < _variableNames.Count; i++)
		{
			if (_variableNames[i] == name)
			{
				_dropdown.Selected = i;
				return;
			}
		}

		// Variable was deleted or renamed. Reset.
		_selectedVariableName = string.Empty;
	}
}
#endif
