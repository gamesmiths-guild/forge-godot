// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that authors an <see cref="Effect"/> instance (scalar or array).
/// </summary>
[Tool]
internal sealed partial class EffectResolverEditor : NodeEditorProperty
{
	private static readonly Type[] _levelExpectedTypes = [typeof(int)];

	private readonly List<ForgeEffectData?> _selectedEffects = [];

	private StatescriptGraph? _graph;
	private Action? _onChanged;
	private bool _isArray;
	private ForgeEffectData? _selectedEffect;

	private List<Func<NodeEditorProperty>> _levelFactories = [];

	private FoldableContainer? _effectDataFoldable;
	private FoldableContainer? _levelFoldable;
	private FoldableContainer? _ownershipFoldable;
	private VBoxContainer? _effectContainer;
	private OptionButton? _levelResolverDropdown;
	private VBoxContainer? _levelEditorContainer;
	private NodeEditorProperty? _levelEditor;
	private OwnershipResolverEditor? _ownershipEditor;

	/// <inheritdoc/>
	public override string DisplayName => "Effect";

	/// <inheritdoc/>
	public override string ResolverTypeId => "Effect";

	/// <inheritdoc/>
	public override bool SupportsArrayValues => true;

	/// <inheritdoc/>
	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(Effect);
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
		_isArray = isArray;

		var resolver = property?.Resolver as EffectResolverResource;
		LoadExistingState(resolver);

		_levelFactories = StatescriptResolverRegistry.GetCompatibleFactories(typeof(int));
		_levelFactories.RemoveAll(factory => !StatescriptResolverRegistry.SupportsScalarValues(factory));

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		CustomMinimumSize = new Vector2(220, 40);

		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		// Effect Data section.
		_effectDataFoldable = new FoldableContainer
		{
			Title = "Effect Data:",
			Folded = resolver?.EffectDataFolded ?? true,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		_effectDataFoldable.FoldingChanged += OnFoldableChanged;
		root.AddChild(_effectDataFoldable);
		_effectContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_effectDataFoldable.AddChild(_effectContainer);
		RebuildEffectContent();

		// Level section.
		_levelFoldable = new FoldableContainer
		{
			Title = "Level:",
			Folded = resolver?.LevelFolded ?? true,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		_levelFoldable.FoldingChanged += OnFoldableChanged;
		root.AddChild(_levelFoldable);
		var levelContent = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_levelFoldable.AddChild(levelContent);
		_levelEditorContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_levelResolverDropdown = CreateLevelDropdown(resolver?.LevelResolver);
		levelContent.AddChild(NestedResolverEditorUtilities.CreateResolverSelectorRow(_levelResolverDropdown));
		levelContent.AddChild(_levelEditorContainer);
		ShowLevelEditor(GetLevelSelectedIndex(resolver?.LevelResolver), resolver?.LevelResolver);
		_levelResolverDropdown.ItemSelected += OnLevelResolverDropdownItemSelected;

		// Ownership section.
		_ownershipFoldable = new FoldableContainer
		{
			Title = "Ownership:",
			Folded = resolver?.OwnershipFolded ?? true,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		_ownershipFoldable.FoldingChanged += OnFoldableChanged;
		root.AddChild(_ownershipFoldable);
		_ownershipEditor = new OwnershipResolverEditor();
		StatescriptNodeProperty? ownershipProperty = resolver?.Ownership is null
			? null
			: new StatescriptNodeProperty { Resolver = resolver.Ownership };
		_ownershipEditor.Setup(graph, ownershipProperty, typeof(EffectOwnership), OnNestedChanged, false);
		_ownershipEditor.LayoutSizeChanged += RaiseLayoutSizeChanged;
		_ownershipFoldable.AddChild(_ownershipEditor);

		UpdateFoldableTitles();
	}

	/// <inheritdoc/>
	public override void SaveTo(StatescriptNodeProperty property)
	{
		var resolver = new EffectResolverResource
		{
			IsArray = _isArray,
			EffectDataFolded = _effectDataFoldable?.Folded ?? false,
			LevelFolded = _levelFoldable?.Folded ?? false,
			OwnershipFolded = _ownershipFoldable?.Folded ?? false,
		};

		if (_isArray)
		{
			var effects = new Array<ForgeEffectData>();

			for (int i = 0; i < _selectedEffects.Count; i++)
			{
				if (_selectedEffects[i] is ForgeEffectData effect)
				{
					effects.Add(effect);
				}
			}

			resolver.Effects = effects;
		}
		else
		{
			resolver.Effect = _selectedEffect;
		}

		if (_levelEditor is not null)
		{
			var levelProperty = new StatescriptNodeProperty();
			_levelEditor.SaveTo(levelProperty);
			resolver.LevelResolver = levelProperty.Resolver;
		}

		if (_ownershipEditor is not null)
		{
			var ownershipProperty = new StatescriptNodeProperty();
			_ownershipEditor.SaveTo(ownershipProperty);
			resolver.Ownership = ownershipProperty.Resolver as OwnershipResolverResource;
		}

		property.Resolver = resolver;
	}

	/// <inheritdoc/>
	public override bool TryGetInlineSummary(out string summary)
	{
		summary = GetEffectSummary();
		return true;
	}

	/// <inheritdoc/>
	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_levelEditor?.ClearCallbacks();
		_ownershipEditor?.ClearCallbacks();
		_effectDataFoldable = null;
		_levelFoldable = null;
		_ownershipFoldable = null;
		_effectContainer = null;
		_levelResolverDropdown = null;
		_levelEditorContainer = null;
		_levelEditor = null;
		_ownershipEditor = null;
	}

	private static EditorResourcePicker CreateEffectPicker(
		ForgeEffectData? effect,
		Action<ForgeEffectData?> onChanged)
	{
		var picker = new EditorResourcePicker
		{
			BaseType = nameof(ForgeEffectData),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			EditedResource = effect,
		};

		picker.ResourceChanged += resource => onChanged(resource as ForgeEffectData);
		return picker;
	}

	private void LoadExistingState(EffectResolverResource? resolver)
	{
		_selectedEffects.Clear();
		_selectedEffect = null;

		if (resolver is null)
		{
			return;
		}

		if (_isArray)
		{
			for (int i = 0; i < resolver.Effects.Count; i++)
			{
				_selectedEffects.Add(resolver.Effects[i]);
			}

			return;
		}

		_selectedEffect = resolver.Effect;
	}

	private OptionButton CreateLevelDropdown(StatescriptResolverResource? existingResolver)
	{
		var dropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };

		foreach (Func<NodeEditorProperty> factory in _levelFactories)
		{
			dropdown.AddItem(StatescriptResolverRegistry.GetDisplayName(factory));
		}

		dropdown.Selected = GetLevelSelectedIndex(existingResolver);
		return dropdown;
	}

	private int GetLevelSelectedIndex(StatescriptResolverResource? existingResolver)
	{
		if (existingResolver is not null)
		{
			for (int i = 0; i < _levelFactories.Count; i++)
			{
				if (StatescriptResolverRegistry.GetResolverTypeId(_levelFactories[i]) == existingResolver.ResolverTypeId)
				{
					return i;
				}
			}
		}

		for (int i = 0; i < _levelFactories.Count; i++)
		{
			if (StatescriptResolverRegistry.GetResolverTypeId(_levelFactories[i]) == "AbilityLevel")
			{
				return i;
			}
		}

		return 0;
	}

	private void ShowLevelEditor(int factoryIndex, StatescriptResolverResource? existingResolver)
	{
		if (_levelEditorContainer is null)
		{
			return;
		}

		NodeEditorProperty? editor = NestedResolverEditorUtilities.CreateNestedEditor(
			_graph,
			_levelFactories,
			factoryIndex,
			existingResolver,
			_levelExpectedTypes,
			OnNestedChanged,
			RaiseLayoutSizeChanged);

		if (editor is null)
		{
			return;
		}

		_levelEditorContainer.AddChild(editor);
		_levelEditor = editor;
	}

	private void OnLevelResolverDropdownItemSelected(long index)
	{
		if (_levelEditorContainer is null)
		{
			return;
		}

		NestedResolverEditorUtilities.ClearContainer(_levelEditorContainer);
		_levelEditor = null;
		ShowLevelEditor((int)index, null);
		NotifyChanged();
		RaiseLayoutSizeChanged();
	}

	private void OnFoldableChanged(bool folded)
	{
		NotifyChanged();
		RaiseLayoutSizeChanged();
	}

	private void OnNestedChanged()
	{
		NotifyChanged();
	}

	private void NotifyChanged()
	{
		UpdateFoldableTitles();
		_onChanged?.Invoke();
	}

	private void UpdateFoldableTitles()
	{
		if (_effectDataFoldable is not null)
		{
			InlineConstantSummaryFormatter.ApplyFoldableTitle(
				"Effect Data:",
				_effectDataFoldable,
				GetEffectSummary(),
				InlineSummaryBadgeKind.Resolver);
		}

		if (_levelFoldable is not null)
		{
			InlineConstantSummaryFormatter.ApplyFoldableTitle("Level:", _levelFoldable, _levelEditor);
		}

		if (_ownershipFoldable is not null)
		{
			InlineConstantSummaryFormatter.ApplyFoldableTitle("Ownership:", _ownershipFoldable, _ownershipEditor);
		}
	}

	private string GetEffectSummary()
	{
		if (_isArray)
		{
			int effectCount = 0;

			for (int i = 0; i < _selectedEffects.Count; i++)
			{
				if (_selectedEffects[i] is not null)
				{
					effectCount++;
				}
			}

			return effectCount switch
			{
				0 => "Empty",
				1 => "1 effect",
				_ => $"{effectCount} effects",
			};
		}

		return _selectedEffect?.Name ?? "(None)";
	}

	private void RebuildEffectContent()
	{
		if (_effectContainer is null)
		{
			return;
		}

		foreach (Node child in _effectContainer.GetChildren())
		{
			_effectContainer.RemoveChild(child);
			child.QueueFree();
		}

		if (_isArray)
		{
			BuildArrayEditor();
			return;
		}

		_effectContainer.AddChild(CreateEffectPicker(
			_selectedEffect,
			effect =>
			{
				_selectedEffect = effect;
				NotifyChanged();
			}));
	}

	private void BuildArrayEditor()
	{
		if (_effectContainer is null)
		{
			return;
		}

		var headerRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_effectContainer.AddChild(headerRow);

		headerRow.AddChild(new Label
		{
			Text = "Effects",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		});

		var addButton = new Button
		{
			Text = "Add",
			SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
		};
		addButton.Pressed += OnAddEffectPressed;
		headerRow.AddChild(addButton);

		for (int i = 0; i < _selectedEffects.Count; i++)
		{
			int capturedIndex = i;
			var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			_effectContainer.AddChild(row);

			row.AddChild(new Label
			{
				Text = $"[{i}]",
				CustomMinimumSize = new Vector2(28, 0),
				HorizontalAlignment = HorizontalAlignment.Right,
			});

			row.AddChild(CreateEffectPicker(
				_selectedEffects[i],
				effect =>
				{
					_selectedEffects[capturedIndex] = effect;
					NotifyChanged();
				}));

			var removeButton = new Button
			{
				Text = "Remove",
				SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
			};
			removeButton.Pressed += () => OnRemoveEffectPressed(capturedIndex);
			row.AddChild(removeButton);
		}
	}

	private void OnAddEffectPressed()
	{
		_selectedEffects.Add(null);
		RebuildEffectContent();
		NotifyChanged();
		RaiseLayoutSizeChanged();
	}

	private void OnRemoveEffectPressed(int index)
	{
		if (index < 0 || index >= _selectedEffects.Count)
		{
			return;
		}

		_selectedEffects.RemoveAt(index);
		RebuildEffectContent();
		NotifyChanged();
		RaiseLayoutSizeChanged();
	}
}
#endif
