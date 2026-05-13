// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Custom node editor for the <c>SetVariableNode</c>. Dynamically filters the Input (value resolver) based on the
/// selected target variable's type. Supports both Graph and Shared variable scopes.
/// </summary>
[Tool]
internal sealed partial class SetVariableNodeEditor : CustomNodeEditor
{
	private const string FoldInputKey = "_fold_input";
	private const string FoldOutputKey = "_fold_output";
	private const string ScopeFoldKey = "_fold_output_scope";
	private const string TargetFoldKey = "_fold_output_target";
	private const string ScopeKey = "_output_scope";

	private readonly List<string> _setPaths = [];
	private readonly List<string> _variableNames = [];

	private StatescriptVariableType? _resolvedType;
	private bool _resolvedIsArray;

	private StatescriptNodeDiscovery.NodeTypeInfo? _cachedTypeInfo;
	private VBoxContainer? _cachedInputEditorContainer;
	private VBoxContainer? _cachedTargetContainer;
	private int _cachedOutputIndex;
	private FoldableContainer? _scopeFoldable;
	private FoldableContainer? _targetFoldable;

	private bool _isSharedScope;

	private OptionButton? _setDropdown;
	private OptionButton? _sharedVarDropdown;
	private string _selectedSetPath = string.Empty;
	private string _selectedSharedVarName = string.Empty;
	private StatescriptVariableType _selectedSharedVarType = StatescriptVariableType.Int;
	private bool _selectedSharedVarIsArray;

	/// <inheritdoc/>
	public override string HandledRuntimeTypeName => "Gamesmiths.Forge.Statescript.Nodes.Action.SetVariableNode";

	/// <inheritdoc/>
	public override void BuildPropertySections(StatescriptNodeDiscovery.NodeTypeInfo typeInfo)
	{
		_cachedTypeInfo = typeInfo;

		bool inputFolded = GetFoldState(FoldInputKey);
		FoldableContainer inputContainer = AddPropertySectionDivider(
			"Input Properties",
			InputPropertyColor,
			FoldInputKey,
			inputFolded);

		var inputEditorContainer = new VBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};

		_cachedInputEditorContainer = inputEditorContainer;

		inputContainer.AddChild(inputEditorContainer);

		bool outputFolded = GetFoldState(FoldOutputKey);
		FoldableContainer outputContainer = AddPropertySectionDivider(
			"Output Variables",
			OutputVariableColor,
			FoldOutputKey,
			outputFolded);

		_resolvedType = null;
		_resolvedIsArray = false;

		StatescriptNodeProperty? outputBinding = FindBinding(StatescriptPropertyDirection.Output, 0);
		_isSharedScope = outputBinding?.Resolver is SharedVariableResolverResource;

		if (outputBinding is null
			&& NodeResource.CustomData.TryGetValue(ScopeKey, out Variant scopeValue))
		{
			_isSharedScope = scopeValue.AsInt32() == (int)VariableScope.Shared;
		}

		ResolveTypeFromBinding(outputBinding);

		if (typeInfo.OutputVariablesInfo.Length > 0)
		{
			AddTargetVariableRow(
				typeInfo.OutputVariablesInfo[0],
				0,
				outputContainer);
		}

		if (typeInfo.InputPropertiesInfo.Length > 0)
		{
			RebuildInputUI(typeInfo.InputPropertiesInfo[0], inputEditorContainer);
		}
	}

	/// <inheritdoc/>
	internal override void Unbind()
	{
		base.Unbind();
		_cachedTypeInfo = null;
		_cachedInputEditorContainer = null;
		_cachedTargetContainer = null;
		_setDropdown = null;
		_sharedVarDropdown = null;
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

	private void ResolveTypeFromBinding(StatescriptNodeProperty? outputBinding)
	{
		_resolvedType = null;
		_resolvedIsArray = false;

		if (outputBinding?.Resolver is VariableResolverResource varRes
			&& !string.IsNullOrEmpty(varRes.VariableName))
		{
			foreach (StatescriptGraphVariable v in Graph.Variables)
			{
				if (v.VariableName == varRes.VariableName)
				{
					_resolvedType = v.VariableType;
					_resolvedIsArray = v.IsArray;
					return;
				}
			}
		}

		if (outputBinding?.Resolver is SharedVariableResolverResource sharedRes
			&& !string.IsNullOrEmpty(sharedRes.VariableName))
		{
			_selectedSetPath = sharedRes.SharedVariableSetPath;
			_selectedSharedVarName = sharedRes.VariableName;
			_selectedSharedVarType = sharedRes.VariableType;
			ResolveSharedVariableType();
			_resolvedType = sharedRes.VariableType;
			_resolvedIsArray = _selectedSharedVarIsArray;
		}
	}

	private void AddTargetVariableRow(
		StatescriptNodeDiscovery.OutputVariableInfo varInfo,
		int index,
		FoldableContainer sectionContainer)
	{
		_cachedOutputIndex = index;

		var outerVBox = new VBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};

		sectionContainer.AddChild(outerVBox);

		_scopeFoldable = new FoldableContainer
		{
			Title = "Scope:",
			Folded = GetFoldState(ScopeFoldKey, true),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		_scopeFoldable.FoldingChanged += OnScopeFoldableFoldingChanged;
		outerVBox.AddChild(_scopeFoldable);

		// Scope toggle row.
		var scopeRow = new HBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};

		_scopeFoldable.AddChild(scopeRow);

		var graphButton = new CheckBox
		{
			Text = "Graph",
			ButtonPressed = !_isSharedScope,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};

		var sharedButton = new CheckBox
		{
			Text = "Shared",
			ButtonPressed = _isSharedScope,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};

		var buttonGroup = new ButtonGroup();
		graphButton.ButtonGroup = buttonGroup;
		sharedButton.ButtonGroup = buttonGroup;

		scopeRow.AddChild(graphButton);
		scopeRow.AddChild(sharedButton);

		_targetFoldable = new FoldableContainer
		{
			Title = $"{varInfo.Label}:",
			Folded = GetFoldState(TargetFoldKey, true),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		_targetFoldable.FoldingChanged += OnTargetFoldableFoldingChanged;
		outerVBox.AddChild(_targetFoldable);

		// Target variable container (rebuilt when scope changes).
		var targetContainer = new VBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};

		_cachedTargetContainer = targetContainer;
		_targetFoldable.AddChild(targetContainer);

		RebuildTargetUI(varInfo, index, targetContainer);
		UpdateScopeFoldableTitle();
		UpdateTargetFoldableTitle(varInfo.Label);

		graphButton.Pressed += () => OnScopeChanged(false, varInfo, index);
		sharedButton.Pressed += () => OnScopeChanged(true, varInfo, index);
	}

	private void OnScopeChanged(
		bool isShared,
		StatescriptNodeDiscovery.OutputVariableInfo varInfo,
		int index)
	{
		if (_isSharedScope == isShared)
		{
			return;
		}

		var oldResolver = FindBinding(StatescriptPropertyDirection.Output, index)?.Resolver?.Duplicate()
			as StatescriptResolverResource;
		var oldInputResolver = FindBinding(StatescriptPropertyDirection.Input, 0)?.Resolver?.Duplicate()
			as StatescriptResolverResource;

		_isSharedScope = isShared;
		NodeResource.CustomData[ScopeKey] = Variant.From(isShared
			? (int)VariableScope.Shared
			: (int)VariableScope.Graph);

		// Clear the output binding since scope changed.
		RemoveBinding(StatescriptPropertyDirection.Output, index);
		_resolvedType = null;
		_resolvedIsArray = false;

		// Reset shared variable state when switching away.
		if (!isShared)
		{
			_selectedSetPath = string.Empty;
			_selectedSharedVarName = string.Empty;
			_selectedSharedVarType = StatescriptVariableType.Int;
			_selectedSharedVarIsArray = false;
		}

		if (_cachedTargetContainer is not null)
		{
			ClearContainer(_cachedTargetContainer);
			RebuildTargetUI(varInfo, index, _cachedTargetContainer);
		}

		// Clear and rebuild input since type changed.
		RemoveBinding(StatescriptPropertyDirection.Input, 0);
		ActiveResolverEditors.Remove(new PropertySlotKey(StatescriptPropertyDirection.Input, 0));

		if (_cachedTypeInfo is not null
			&& _cachedInputEditorContainer is not null
			&& _cachedTypeInfo.InputPropertiesInfo.Length > 0)
		{
			RebuildInputUI(_cachedTypeInfo.InputPropertiesInfo[0], _cachedInputEditorContainer);
		}

		var newResolver = FindBinding(StatescriptPropertyDirection.Output, index)?.Resolver?.Duplicate()
			as StatescriptResolverResource;
		var newInputResolver = FindBinding(StatescriptPropertyDirection.Input, 0)?.Resolver?.Duplicate()
			as StatescriptResolverResource;

		RecordResolverBindingChange(
			StatescriptPropertyDirection.Output,
			index,
			oldResolver,
			newResolver,
			"Change Variable Scope");

		RecordResolverBindingChange(
			StatescriptPropertyDirection.Input,
			0,
			oldInputResolver,
			newInputResolver,
			"Change Variable Scope Input");

		UpdateScopeFoldableTitle();
		UpdateTargetFoldableTitle(varInfo.Label);
		RaisePropertyBindingChanged();
		ResetSize();
	}

	private void RebuildTargetUI(
		StatescriptNodeDiscovery.OutputVariableInfo varInfo,
		int index,
		VBoxContainer container)
	{
		if (_isSharedScope)
		{
			BuildSharedVariableUI(varInfo, container);
		}
		else
		{
			BuildGraphVariableUI(varInfo, index, container);
		}
	}

	private void BuildGraphVariableUI(
		StatescriptNodeDiscovery.OutputVariableInfo varInfo,
		int index,
		VBoxContainer container)
	{
		var hBox = new HBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};

		container.AddChild(hBox);

		var nameLabel = new Label
		{
			Text = varInfo.Label,
			CustomMinimumSize = new Vector2(60, 0),
		};

		nameLabel.AddThemeColorOverride("font_color", OutputVariableColor);
		hBox.AddChild(nameLabel);

		var dropdown = new OptionButton
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};

		dropdown.SetMeta("is_variable_dropdown", true);

		dropdown.AddItem("(None)");

		foreach (StatescriptGraphVariable v in Graph.Variables)
		{
			dropdown.AddItem(v.VariableName);
		}

		StatescriptNodeProperty? binding = FindBinding(StatescriptPropertyDirection.Output, index);
		int selectedIndex = 0;

		if (binding?.Resolver is VariableResolverResource varRes
			&& !string.IsNullOrEmpty(varRes.VariableName))
		{
			for (int i = 0; i < Graph.Variables.Count; i++)
			{
				if (Graph.Variables[i].VariableName == varRes.VariableName)
				{
					selectedIndex = i + 1;
					break;
				}
			}
		}

		dropdown.Selected = selectedIndex;

		if (selectedIndex == 0)
		{
			RemoveBinding(StatescriptPropertyDirection.Output, index);
		}

		dropdown.ItemSelected += OnTargetVariableDropdownItemSelected;

		hBox.AddChild(dropdown);
	}

	private void BuildSharedVariableUI(
		StatescriptNodeDiscovery.OutputVariableInfo varInfo,
		VBoxContainer container)
	{
		var nameLabel = new Label
		{
			Text = varInfo.Label,
			CustomMinimumSize = new Vector2(60, 0),
		};

		nameLabel.AddThemeColorOverride("font_color", OutputVariableColor);
		container.AddChild(nameLabel);

		// Set dropdown row.
		var setRow = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		container.AddChild(setRow);

		setRow.AddChild(new Label
		{
			Text = "Set:",
			CustomMinimumSize = new Vector2(60, 0),
			HorizontalAlignment = HorizontalAlignment.Right,
		});

		_setDropdown = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		PopulateSetDropdown();
		setRow.AddChild(_setDropdown);

		// Variable dropdown row.
		var varRow = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		container.AddChild(varRow);

		varRow.AddChild(new Label
		{
			Text = "Var:",
			CustomMinimumSize = new Vector2(60, 0),
			HorizontalAlignment = HorizontalAlignment.Right,
		});

		_sharedVarDropdown = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		_sharedVarDropdown.SetMeta("is_shared_variable_dropdown", true);
		PopulateSharedVariableDropdown();
		varRow.AddChild(_sharedVarDropdown);

		_setDropdown.ItemSelected += OnSharedSetDropdownItemSelected;
		_sharedVarDropdown.ItemSelected += OnSharedVariableDropdownItemSelected;
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

		// Restore selection.
		for (int i = 0; i < _setPaths.Count; i++)
		{
			if (_setPaths[i] == _selectedSetPath)
			{
				_setDropdown.Selected = i;
				return;
			}
		}

		_setDropdown.Selected = 0;
		_selectedSetPath = string.Empty;
	}

	private void PopulateSharedVariableDropdown()
	{
		if (_sharedVarDropdown is null)
		{
			return;
		}

		_sharedVarDropdown.SetMeta("shared_variable_set_path", _selectedSetPath);

		_sharedVarDropdown.Clear();
		_variableNames.Clear();

		_sharedVarDropdown.AddItem("(None)");
		_variableNames.Add(string.Empty);

		if (!string.IsNullOrEmpty(_selectedSetPath) && ResourceLoader.Exists(_selectedSetPath))
		{
			ForgeSharedVariableSet? set = ResourceLoader.Load<ForgeSharedVariableSet>(_selectedSetPath);

			if (set is not null)
			{
				foreach (string? variableName in set.Variables.Select(x => x.VariableName))
				{
					if (string.IsNullOrEmpty(variableName))
					{
						continue;
					}

					_sharedVarDropdown.AddItem(variableName);
					_variableNames.Add(variableName);
				}
			}
		}

		// Restore selection.
		for (int i = 0; i < _variableNames.Count; i++)
		{
			if (_variableNames[i] == _selectedSharedVarName)
			{
				_sharedVarDropdown.Selected = i;
				return;
			}
		}

		_sharedVarDropdown.Selected = 0;
		_selectedSharedVarName = string.Empty;
	}

	private void OnSharedSetDropdownItemSelected(long x)
	{
		if (_setDropdown is null)
		{
			return;
		}

		int idx = _setDropdown.Selected;

		var oldResolver = FindBinding(StatescriptPropertyDirection.Output, _cachedOutputIndex)?.Resolver?.Duplicate()
			as StatescriptResolverResource;
		var oldInputResolver = FindBinding(StatescriptPropertyDirection.Input, 0)?.Resolver?.Duplicate()
			as StatescriptResolverResource;

		_selectedSetPath = idx >= 0 && idx < _setPaths.Count ? _setPaths[idx] : string.Empty;
		_selectedSharedVarName = string.Empty;
		_selectedSharedVarType = StatescriptVariableType.Int;

		PopulateSharedVariableDropdown();
		UpdateSharedOutputBinding();

		var newResolver = FindBinding(StatescriptPropertyDirection.Output, _cachedOutputIndex)?.Resolver?.Duplicate()
			as StatescriptResolverResource;

		StatescriptVariableType? previousType = _resolvedType;
		_resolvedType = null;
		_resolvedIsArray = false;

		if (previousType != _resolvedType)
		{
			RemoveBinding(StatescriptPropertyDirection.Input, 0);
			ActiveResolverEditors.Remove(new PropertySlotKey(StatescriptPropertyDirection.Input, 0));
		}

		if (_cachedTypeInfo is not null
			&& _cachedInputEditorContainer is not null
			&& _cachedTypeInfo.InputPropertiesInfo.Length > 0)
		{
			RebuildInputUI(_cachedTypeInfo.InputPropertiesInfo[0], _cachedInputEditorContainer);
		}

		var newInputResolver = FindBinding(StatescriptPropertyDirection.Input, 0)?.Resolver?.Duplicate()
			as StatescriptResolverResource;

		RecordResolverBindingChange(
			StatescriptPropertyDirection.Output,
			_cachedOutputIndex,
			oldResolver,
			newResolver,
			"Change Shared Variable Set");

		if (previousType != _resolvedType)
		{
			RecordResolverBindingChange(
				StatescriptPropertyDirection.Input,
				0,
				oldInputResolver,
				newInputResolver,
				"Change Shared Variable Set Input");
		}

		RaisePropertyBindingChanged();
		ResetSize();
	}

	private void OnSharedVariableDropdownItemSelected(long x)
	{
		if (_sharedVarDropdown is null)
		{
			return;
		}

		int idx = _sharedVarDropdown.Selected;

		var oldResolver = FindBinding(StatescriptPropertyDirection.Output, _cachedOutputIndex)?.Resolver?.Duplicate()
			as StatescriptResolverResource;
		var oldInputResolver = FindBinding(StatescriptPropertyDirection.Input, 0)?.Resolver?.Duplicate()
			as StatescriptResolverResource;

		StatescriptVariableType? previousType = _resolvedType;
		bool previousIsArray = _resolvedIsArray;

		if (idx >= 0 && idx < _variableNames.Count)
		{
			_selectedSharedVarName = _variableNames[idx];
			ResolveSharedVariableType();
		}
		else
		{
			_selectedSharedVarName = string.Empty;
			_selectedSharedVarType = StatescriptVariableType.Int;
			_selectedSharedVarIsArray = false;
		}

		UpdateSharedOutputBinding();

		if (!string.IsNullOrEmpty(_selectedSharedVarName))
		{
			_resolvedType = _selectedSharedVarType;
			_resolvedIsArray = _selectedSharedVarIsArray;
		}
		else
		{
			_resolvedType = null;
			_resolvedIsArray = false;
		}

		if (previousType != _resolvedType || previousIsArray != _resolvedIsArray)
		{
			RemoveBinding(StatescriptPropertyDirection.Input, 0);
			ActiveResolverEditors.Remove(new PropertySlotKey(StatescriptPropertyDirection.Input, 0));
		}

		if (_cachedTypeInfo is not null
			&& _cachedInputEditorContainer is not null
			&& _cachedTypeInfo.InputPropertiesInfo.Length > 0)
		{
			RebuildInputUI(_cachedTypeInfo.InputPropertiesInfo[0], _cachedInputEditorContainer);
		}

		var newResolver = FindBinding(StatescriptPropertyDirection.Output, _cachedOutputIndex)?.Resolver?.Duplicate()
			as StatescriptResolverResource;
		var newInputResolver = FindBinding(StatescriptPropertyDirection.Input, 0)?.Resolver?.Duplicate()
			as StatescriptResolverResource;

		RecordResolverBindingChange(
			StatescriptPropertyDirection.Output,
			_cachedOutputIndex,
			oldResolver,
			newResolver,
			"Change Shared Target Variable");

		if (previousType != _resolvedType || previousIsArray != _resolvedIsArray)
		{
			RecordResolverBindingChange(
				StatescriptPropertyDirection.Input,
				0,
				oldInputResolver,
				newInputResolver,
				"Change Shared Target Variable Input");
		}

		if (_cachedTypeInfo?.OutputVariablesInfo.Length > 0)
		{
			UpdateTargetFoldableTitle(_cachedTypeInfo.OutputVariablesInfo[0].Label);
		}

		RaisePropertyBindingChanged();
		ResetSize();
	}

	private void UpdateSharedOutputBinding()
	{
		if (string.IsNullOrEmpty(_selectedSharedVarName) || string.IsNullOrEmpty(_selectedSetPath))
		{
			RemoveBinding(StatescriptPropertyDirection.Output, _cachedOutputIndex);
			return;
		}

		EnsureBinding(StatescriptPropertyDirection.Output, _cachedOutputIndex).Resolver =
			new SharedVariableResolverResource
			{
				SharedVariableSetPath = _selectedSetPath,
				VariableName = _selectedSharedVarName,
				VariableType = _selectedSharedVarType,
			};
	}

	private void ResolveSharedVariableType()
	{
		if (string.IsNullOrEmpty(_selectedSetPath)
			|| string.IsNullOrEmpty(_selectedSharedVarName)
			|| !ResourceLoader.Exists(_selectedSetPath))
		{
			_selectedSharedVarType = StatescriptVariableType.Int;
			_selectedSharedVarIsArray = false;
			return;
		}

		ForgeSharedVariableSet? set = ResourceLoader.Load<ForgeSharedVariableSet>(_selectedSetPath);

		if (set is null)
		{
			_selectedSharedVarType = StatescriptVariableType.Int;
			_selectedSharedVarIsArray = false;
			return;
		}

		foreach (ForgeSharedVariableDefinition def in set.Variables)
		{
			if (def.VariableName == _selectedSharedVarName)
			{
				_selectedSharedVarType = def.VariableType;
				_selectedSharedVarIsArray = def.IsArray;
				return;
			}
		}

		_selectedSharedVarType = StatescriptVariableType.Int;
		_selectedSharedVarIsArray = false;
	}

	private void OnTargetVariableDropdownItemSelected(long x)
	{
		if (_cachedTypeInfo is null || _cachedInputEditorContainer is null)
		{
			return;
		}

		int index = _cachedOutputIndex;
		int variableIndex = (int)x - 1;

		StatescriptVariableType? previousType = _resolvedType;
		bool previousIsArray = _resolvedIsArray;

		var oldOutputResolver = FindBinding(StatescriptPropertyDirection.Output, index)?.Resolver?.Duplicate()
			as StatescriptResolverResource;
		var oldInputResolver = FindBinding(StatescriptPropertyDirection.Input, 0)?.Resolver?.Duplicate()
			as StatescriptResolverResource;

		if (variableIndex < 0)
		{
			RemoveBinding(StatescriptPropertyDirection.Output, index);
			_resolvedType = null;
			_resolvedIsArray = false;
		}
		else
		{
			string variableName = Graph.Variables[variableIndex].VariableName;
			EnsureBinding(StatescriptPropertyDirection.Output, index).Resolver =
				new VariableResolverResource { VariableName = variableName };

			_resolvedType = Graph.Variables[variableIndex].VariableType;
			_resolvedIsArray = Graph.Variables[variableIndex].IsArray;
		}

		if (previousType != _resolvedType || previousIsArray != _resolvedIsArray)
		{
			RemoveBinding(StatescriptPropertyDirection.Input, 0);

			var inputKey = new PropertySlotKey(StatescriptPropertyDirection.Input, 0);

			ActiveResolverEditors.Remove(inputKey);
		}

		if (_cachedTypeInfo.InputPropertiesInfo.Length > 0)
		{
			RebuildInputUI(_cachedTypeInfo.InputPropertiesInfo[0], _cachedInputEditorContainer);
		}

		var newOutputResolver = FindBinding(StatescriptPropertyDirection.Output, index)?.Resolver?.Duplicate()
			as StatescriptResolverResource;
		var newInputResolver = FindBinding(StatescriptPropertyDirection.Input, 0)?.Resolver?.Duplicate()
			as StatescriptResolverResource;

		RecordResolverBindingChange(
			StatescriptPropertyDirection.Output,
			index,
			oldOutputResolver,
			newOutputResolver,
			"Change Target Variable");

		if (previousType != _resolvedType || previousIsArray != _resolvedIsArray)
		{
			RecordResolverBindingChange(
				StatescriptPropertyDirection.Input,
				0,
				oldInputResolver,
				newInputResolver,
				"Change Target Variable Input");
		}

		if (_cachedTypeInfo?.OutputVariablesInfo.Length > 0)
		{
			UpdateTargetFoldableTitle(_cachedTypeInfo.OutputVariablesInfo[0].Label);
		}

		RaisePropertyBindingChanged();
		ResetSize();
	}

	private void OnScopeFoldableFoldingChanged(bool folded)
	{
		SetFoldStateWithUndo(ScopeFoldKey, folded);
		UpdateScopeFoldableTitle();
		RaisePropertyBindingChanged();
		ResetSize();
	}

	private void OnTargetFoldableFoldingChanged(bool folded)
	{
		SetFoldStateWithUndo(TargetFoldKey, folded);

		if (_cachedTypeInfo?.OutputVariablesInfo.Length > 0)
		{
			UpdateTargetFoldableTitle(_cachedTypeInfo.OutputVariablesInfo[0].Label);
		}

		RaisePropertyBindingChanged();
		ResetSize();
	}

	private void UpdateScopeFoldableTitle()
	{
		if (_scopeFoldable is null)
		{
			return;
		}

		InlineConstantSummaryFormatter.ApplyFoldableTitle(
			"Scope:",
			_scopeFoldable,
			_isSharedScope ? "Shared" : "Graph",
			InlineSummaryBadgeKind.Enum);
	}

	private void UpdateTargetFoldableTitle(string label)
	{
		if (_targetFoldable is null)
		{
			return;
		}

		string summary = _isSharedScope
			? _selectedSharedVarName
			: GetSelectedGraphVariableName();

		InlineSummaryBadgeKind badgeKind = _isSharedScope
			? InlineSummaryBadgeKind.SharedVariable
			: InlineSummaryBadgeKind.Variable;

		string highlightedVariableName = !_isSharedScope
			? summary
			: string.Empty;
		string highlightedSharedVariableSetPath = _isSharedScope ? _selectedSetPath : string.Empty;
		string highlightedSharedVariableName = _isSharedScope ? summary : string.Empty;

		InlineConstantSummaryFormatter.ApplyFoldableTitle(
			$"{label}:",
			_targetFoldable,
			string.IsNullOrWhiteSpace(summary) ? "(None)" : summary,
			badgeKind,
			highlightedVariableName: highlightedVariableName,
			highlightedSharedVariableSetPath: highlightedSharedVariableSetPath,
			highlightedSharedVariableName: highlightedSharedVariableName);
	}

	private string GetSelectedGraphVariableName()
	{
		if (_cachedOutputIndex < 0)
		{
			return string.Empty;
		}

		if (FindBinding(StatescriptPropertyDirection.Output, _cachedOutputIndex)?.Resolver
			is VariableResolverResource varRes)
		{
			return varRes.VariableName;
		}

		return string.Empty;
	}

	private void RebuildInputUI(
		StatescriptNodeDiscovery.InputPropertyInfo propInfo,
		VBoxContainer container)
	{
		ClearContainer(container);

		if (_resolvedType is null)
		{
			var placeholder = new Label
			{
				Text = "Select target variable first",
				HorizontalAlignment = HorizontalAlignment.Center,
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			};

			placeholder.AddThemeColorOverride("font_color", new Color(1, 1, 1, 0.4f));
			container.AddChild(placeholder);
			ResetSize();
			return;
		}

		Type resolvedClrType = StatescriptVariableTypeConverter.ToSystemType(_resolvedType.Value);

		AddInputPropertyRow(
			new StatescriptNodeDiscovery.InputPropertyInfo(propInfo.Label, resolvedClrType, _resolvedIsArray),
			0,
			container);

		ResetSize();
	}
}
#endif
