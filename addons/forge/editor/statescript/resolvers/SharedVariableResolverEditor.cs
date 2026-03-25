// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
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
	private readonly List<string> _setPaths = [];
	private readonly List<string> _setDisplayNames = [];
	private readonly List<string> _variableNames = [];

	private OptionButton? _setDropdown;
	private OptionButton? _variableDropdown;
	private Action? _onChanged;
	private Type _expectedType = typeof(Variant128);

	private string _selectedSetPath = string.Empty;
	private string _selectedVariableName = string.Empty;
	private StatescriptVariableType _selectedVariableType = StatescriptVariableType.Int;

	/// <inheritdoc/>
	public override string DisplayName => "Shared Variable";

	/// <inheritdoc/>
	public override string ResolverTypeId => "SharedVariable";

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
		_expectedType = expectedType;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var vBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(vBox);

		if (property?.Resolver is SharedVariableResolverResource sharedRes)
		{
			_selectedSetPath = sharedRes.SharedVariableSetPath;
			_selectedVariableName = sharedRes.VariableName;
			_selectedVariableType = sharedRes.VariableType;
		}

		var setRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		vBox.AddChild(setRow);

		setRow.AddChild(new Label
		{
			Text = "Set:",
			CustomMinimumSize = new Vector2(45, 0),
			HorizontalAlignment = HorizontalAlignment.Right,
		});

		_setDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		PopulateSetDropdown();
		setRow.AddChild(_setDropdown);

		var varRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		vBox.AddChild(varRow);

		varRow.AddChild(new Label
		{
			Text = "Var:",
			CustomMinimumSize = new Vector2(45, 0),
			HorizontalAlignment = HorizontalAlignment.Right,
		});

		_variableDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		PopulateVariableDropdown();
		varRow.AddChild(_variableDropdown);

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
		};
	}

	/// <inheritdoc/>
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
		for (var i = 0; i < dir.GetFileCount(); i++)
		{
			var path = dir.GetFilePath(i);

			if (!path.EndsWith(".tres", StringComparison.InvariantCultureIgnoreCase)
				&& !path.EndsWith(".res", StringComparison.InvariantCultureIgnoreCase))
			{
				continue;
			}

			Resource resource = ResourceLoader.Load(path);

			if (resource is ForgeSharedVariableSet)
			{
				GD.Print($"Found ForgeSharedVariableSet: {path}");
				results.Add(path);
			}
		}

		for (var i = 0; i < dir.GetSubdirCount(); i++)
		{
			ScanFilesystemDirectory(dir.GetSubdir(i), results);
		}
	}

	private void OnSetDropdownItemSelected(long index)
	{
		if (_setDropdown is null)
		{
			return;
		}

		var idx = _setDropdown.Selected;
		_selectedSetPath = idx >= 0 && idx < _setPaths.Count ? _setPaths[idx] : string.Empty;
		_selectedVariableName = string.Empty;
		_selectedVariableType = StatescriptVariableType.Int;

		PopulateVariableDropdown();

		_onChanged?.Invoke();
	}

	private void OnVariableDropdownItemSelected(long index)
	{
		if (_variableDropdown is null)
		{
			return;
		}

		var idx = _variableDropdown.Selected;

		if (idx >= 0 && idx < _variableNames.Count)
		{
			_selectedVariableName = _variableNames[idx];
			ResolveVariableType();
		}
		else
		{
			_selectedVariableName = string.Empty;
			_selectedVariableType = StatescriptVariableType.Int;
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
		_setDisplayNames.Clear();

		_setDropdown.AddItem("(None)");
		_setPaths.Add(string.Empty);
		_setDisplayNames.Add("(None)");

		foreach (var path in FindAllSharedVariableSetPaths())
		{
			var displayName = path[(path.LastIndexOf('/') + 1)..];

			if (displayName.EndsWith(".tres", StringComparison.OrdinalIgnoreCase))
			{
				displayName = displayName[..^5];
			}

			_setDropdown.AddItem(displayName);
			_setPaths.Add(path);
			_setDisplayNames.Add(displayName);
		}

		// Restore selection.
		for (var i = 0; i < _setPaths.Count; i++)
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

	private void PopulateVariableDropdown()
	{
		if (_variableDropdown is null)
		{
			return;
		}

		_variableDropdown.Clear();
		_variableNames.Clear();

		_variableDropdown.AddItem("(None)");
		_variableNames.Add(string.Empty);

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

					if (_expectedType != typeof(Variant128)
						&& !StatescriptVariableTypeConverter.IsCompatible(_expectedType, def.VariableType))
					{
						continue;
					}

					var label = $"{def.VariableName}";
					_variableDropdown.AddItem(label);
					_variableNames.Add(def.VariableName);
				}
			}
		}

		// Restore selection.
		for (var i = 0; i < _variableNames.Count; i++)
		{
			if (_variableNames[i] == _selectedVariableName)
			{
				_variableDropdown.Selected = i;
				return;
			}
		}

		_variableDropdown.Selected = 0;
		_selectedVariableName = string.Empty;
	}

	private void ResolveVariableType()
	{
		if (string.IsNullOrEmpty(_selectedSetPath)
			|| string.IsNullOrEmpty(_selectedVariableName)
			|| !ResourceLoader.Exists(_selectedSetPath))
		{
			_selectedVariableType = StatescriptVariableType.Int;
			return;
		}

		ForgeSharedVariableSet? set = ResourceLoader.Load<ForgeSharedVariableSet>(_selectedSetPath);

		if (set is null)
		{
			_selectedVariableType = StatescriptVariableType.Int;
			return;
		}

		foreach (ForgeSharedVariableDefinition def in set.Variables)
		{
			if (def.VariableName == _selectedVariableName)
			{
				_selectedVariableType = def.VariableType;
				return;
			}
		}

		_selectedVariableType = StatescriptVariableType.Int;
	}
}
#endif
