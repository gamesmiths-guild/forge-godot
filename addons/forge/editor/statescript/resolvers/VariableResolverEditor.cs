// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that binds an input property to a graph variable. Only variables whose type is compatible with the
/// expected type are shown in the dropdown.
/// </summary>
[Tool]
internal sealed partial class VariableResolverEditor : NodeEditorProperty
{
	private readonly List<string> _variableNames = [];

	private OptionButton? _dropdown;
	private string _selectedVariableName = string.Empty;
	private Action? _onChanged;

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
		_onChanged = onChanged;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		CustomMinimumSize = new Vector2(200, 25);

		_dropdown = new OptionButton
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(100, 0),
		};

		_dropdown.SetMeta("is_variable_dropdown", true);

		PopulateDropdown(graph, expectedType);

		if (property?.Resolver is VariableResolverResource varRes
			&& !string.IsNullOrEmpty(varRes.VariableName))
		{
			_selectedVariableName = varRes.VariableName;
			SelectByName(varRes.VariableName);
		}

		_dropdown.ItemSelected += OnDropdownItemSelected;

		AddChild(_dropdown);
	}

	/// <inheritdoc/>
	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new VariableResolverResource
		{
			VariableName = _selectedVariableName,
		};
	}

	/// <inheritdoc/>
	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
	}

	private void OnDropdownItemSelected(long index)
	{
		if (_dropdown is null)
		{
			return;
		}

		var idx = _dropdown.Selected;
		_selectedVariableName = idx >= 0 && idx < _variableNames.Count ? _variableNames[idx] : string.Empty;
		_onChanged?.Invoke();
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

		_selectedVariableName = string.Empty;
	}
}
#endif
