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
/// Resolver editor that binds a node input property to a shared variable on the owning entity. Uses a two-step
/// selection: first select the <see cref="ForgeSharedVariableSet"/> resource, then select a compatible variable from
/// that set. At runtime the value is read from the entity's <see cref="GraphContext.SharedVariables"/> bag.
/// </summary>
[Tool]
internal sealed partial class SharedVariableResolverEditor : NodeEditorProperty
{
	private const float LabelWidth = 45.0f;

	private readonly List<string> _setPaths = [];
	private readonly List<string> _variableNames = [];

	private OptionButton? _setDropdown;
	private OptionButton? _variableDropdown;
	private Action? _onChanged;
	private Type _expectedType = typeof(Variant128);

	private string _selectedSetPath = string.Empty;
	private string _selectedVariableName = string.Empty;
	private StatescriptVariableType _selectedVariableType = StatescriptVariableType.Int;
	private bool _selectedIsArray;

	/// <inheritdoc/>
	public override string DisplayName => "Shared Variable";

	/// <inheritdoc/>
	public override string ResolverTypeId => "SharedVariable";

	/// <inheritdoc/>
	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType != typeof(IForgeEntity);
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
		_expectedType = expectedType;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var vBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(vBox);

		if (property?.Resolver is SharedVariableResolverResource sharedRes)
		{
			_selectedSetPath = sharedRes.SharedVariableSetPath;
			_selectedVariableName = sharedRes.VariableName;
			_selectedVariableType = sharedRes.VariableType;
			_selectedIsArray = sharedRes.IsArray;
		}

		_setDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		PopulateSetDropdown();
		vBox.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow("Set:", _setDropdown, LabelWidth));

		_variableDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_variableDropdown.SetMeta("is_shared_variable_dropdown", true);
		PopulateVariableDropdown();
		vBox.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow("Var:", _variableDropdown, LabelWidth));

		_setDropdown.ItemSelected += OnSetDropdownItemSelected;
		_variableDropdown.ItemSelected += OnVariableDropdownItemSelected;
	}

	/// <inheritdoc/>
	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new SharedVariableResolverResource
		{
			SharedVariableSetPath = _selectedSetPath,
			VariableName = _selectedVariableName,
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
		return InlineSummaryBadgeKind.SharedVariable;
	}

	public override bool TryGetHighlightedVariableName(out string variableName)
	{
		variableName = string.Empty;
		return false;
	}

	public override bool TryGetHighlightedSharedVariable(out string sharedVariableSetPath, out string variableName)
	{
		sharedVariableSetPath = _selectedSetPath;
		variableName = _selectedVariableName;
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

	private void OnSetDropdownItemSelected(long index)
	{
		if (_setDropdown is null)
		{
			return;
		}

		int idx = _setDropdown.Selected;
		_selectedSetPath = idx >= 0 && idx < _setPaths.Count ? _setPaths[idx] : string.Empty;
		_selectedVariableName = string.Empty;
		_selectedVariableType = StatescriptVariableType.Int;
		_selectedIsArray = false;

		PopulateVariableDropdown();

		_onChanged?.Invoke();
	}

	private void OnVariableDropdownItemSelected(long index)
	{
		if (_variableDropdown is null)
		{
			return;
		}

		int idx = _variableDropdown.Selected;

		if (idx >= 0 && idx < _variableNames.Count)
		{
			_selectedVariableName = _variableNames[idx];
			ResolveVariableType();
		}
		else
		{
			_selectedVariableName = string.Empty;
			_selectedVariableType = StatescriptVariableType.Int;
			_selectedIsArray = false;
		}

		_onChanged?.Invoke();
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
			string displayName = VariableResolverEditorUtilities.GetResourceDisplayName(path);
			_setDropdown.AddItem(displayName);
			_setPaths.Add(path);
		}

		ResolverEditorLayoutUtilities.RestoreSelection(_setDropdown, _setPaths, _selectedSetPath);
		if (_setDropdown.Selected == 0)
		{
			_selectedSetPath = string.Empty;
		}
	}

	private void PopulateVariableDropdown()
	{
		if (_variableDropdown is null)
		{
			return;
		}

		_variableDropdown.SetMeta("shared_variable_set_path", _selectedSetPath);

		_variableDropdown.Clear();
		_variableNames.Clear();

		_variableDropdown.AddItem("(None)");
		_variableNames.Add(string.Empty);

		Type[] allowedExpectedTypes = GetAllowedExpectedTypes(_expectedType);

		if (!string.IsNullOrEmpty(_selectedSetPath) && ResourceLoader.Exists(_selectedSetPath))
		{
			ForgeSharedVariableSet? set = ResourceLoader.Load<ForgeSharedVariableSet>(_selectedSetPath);

			if (set is not null)
			{
				foreach (ForgeSharedVariableDefinition def in set.Variables)
				{
					if (string.IsNullOrEmpty(def.VariableName))
					{
						continue;
					}

					if (!IsCompatibleType(allowedExpectedTypes, def.VariableType))
					{
						continue;
					}

					string label = $"{def.VariableName}";
					_variableDropdown.AddItem(label);
					_variableNames.Add(def.VariableName);
				}
			}
		}

		ResolverEditorLayoutUtilities.RestoreSelection(_variableDropdown, _variableNames, _selectedVariableName);
		if (_variableDropdown.Selected == 0)
		{
			_selectedVariableName = string.Empty;
		}
	}

	private void ResolveVariableType()
	{
		if (string.IsNullOrEmpty(_selectedSetPath)
			|| string.IsNullOrEmpty(_selectedVariableName)
			|| !ResourceLoader.Exists(_selectedSetPath))
		{
			_selectedVariableType = StatescriptVariableType.Int;
			_selectedIsArray = false;
			return;
		}

		ForgeSharedVariableSet? set = ResourceLoader.Load<ForgeSharedVariableSet>(_selectedSetPath);

		if (set is null)
		{
			_selectedVariableType = StatescriptVariableType.Int;
			_selectedIsArray = false;
			return;
		}

		foreach (ForgeSharedVariableDefinition def in set.Variables)
		{
			if (def.VariableName == _selectedVariableName)
			{
				_selectedVariableType = def.VariableType;
				_selectedIsArray = def.IsArray;
				return;
			}
		}

		_selectedVariableType = StatescriptVariableType.Int;
		_selectedIsArray = false;
	}
}
#endif
