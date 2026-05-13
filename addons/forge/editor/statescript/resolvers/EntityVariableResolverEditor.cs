// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class EntityVariableResolverEditor : NodeEditorProperty
{
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
		root.AddChild(CreateLabeledRow("Scope:", _scopeDropdown));

		_graphVariableDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_graphVariableDropdown.SetMeta("is_variable_dropdown", true);
		_graphVariableDropdown.ItemSelected += OnGraphVariableChanged;
		_graphRow = CreateLabeledRow("Var:", _graphVariableDropdown);
		root.AddChild(_graphRow);

		_setDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_setDropdown.ItemSelected += OnSetChanged;
		_setRow = CreateLabeledRow("Set:", _setDropdown);
		root.AddChild(_setRow);

		_sharedVariableDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_sharedVariableDropdown.SetMeta("is_shared_variable_dropdown", true);
		_sharedVariableDropdown.ItemSelected += OnSharedVariableChanged;
		_sharedVariableRow = CreateLabeledRow("Var:", _sharedVariableDropdown);
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

	private static List<string> FindAllSharedVariableSetPaths()
	{
		var results = new List<string>();
		EditorFileSystemDirectory root = EditorInterface.Singleton.GetResourceFilesystem().GetFilesystem();
		ScanFilesystemDirectory(root, results);
		return results;
	}

	private static void ScanFilesystemDirectory(EditorFileSystemDirectory dir, List<string> results)
	{
		for (int i = 0; i < dir.GetFileCount(); i++)
		{
			string path = dir.GetFilePath(i);
			if (!path.EndsWith(".tres", StringComparison.InvariantCultureIgnoreCase)
				&& !path.EndsWith(".res", StringComparison.InvariantCultureIgnoreCase))
			{
				continue;
			}

			Resource resource = ResourceLoader.Load(path);
			if (resource is ForgeSharedVariableSet)
			{
				results.Add(path);
			}
		}

		for (int i = 0; i < dir.GetSubdirCount(); i++)
		{
			ScanFilesystemDirectory(dir.GetSubdir(i), results);
		}
	}

	private static HBoxContainer CreateLabeledRow(string labelText, Control editor)
	{
		var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		row.AddChild(new Label
		{
			Text = labelText,
			CustomMinimumSize = new Vector2(45, 0),
			HorizontalAlignment = HorizontalAlignment.Right,
		});
		row.AddChild(editor);
		return row;
	}

	private static void RestoreSelection(OptionButton dropdown, List<string> values, string selectedValue)
	{
		for (int i = 0; i < values.Count; i++)
		{
			if (values[i] == selectedValue)
			{
				dropdown.Selected = i;
				return;
			}
		}

		dropdown.Selected = 0;
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

		RestoreSelection(_graphVariableDropdown, _graphVariableNames, _selectedVariableName);
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

		foreach (string path in FindAllSharedVariableSetPaths())
		{
			string displayName = path[(path.LastIndexOf('/') + 1)..];
			if (displayName.EndsWith(".tres", StringComparison.OrdinalIgnoreCase))
			{
				displayName = displayName[..^5];
			}

			_setDropdown.AddItem(displayName);
			_setPaths.Add(path);
		}

		RestoreSelection(_setDropdown, _setPaths, _selectedSetPath);
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

		RestoreSelection(_sharedVariableDropdown, _sharedVariableNames, _selectedVariableName);
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
