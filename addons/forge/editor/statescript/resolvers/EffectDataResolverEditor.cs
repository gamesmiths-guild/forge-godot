// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor for scalar and array <see cref="ForgeEffectData"/> selections.
/// </summary>
[Tool]
internal sealed partial class EffectDataResolverEditor : NodeEditorProperty
{
	private readonly List<ForgeEffectData?> _selectedEffects = [];

	private bool _isArray;
	private Action? _onChanged;
	private ForgeEffectData? _selectedEffect;
	private VBoxContainer? _contentContainer;

	/// <inheritdoc/>
	public override string DisplayName => "Effect";

	/// <inheritdoc/>
	public override string ResolverTypeId => "EffectData";

	/// <inheritdoc/>
	public override bool SupportsArrayValues => true;

	/// <inheritdoc/>
	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(EffectData);
	}

	/// <inheritdoc/>
	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		_isArray = isArray;
		_onChanged = onChanged;

		LoadExistingState(property?.Resolver as EffectDataResolverResource);

		CustomMinimumSize = new Vector2(220, 40);

		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		_contentContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		root.AddChild(_contentContainer);

		RebuildContent();
	}

	/// <inheritdoc/>
	public override void SaveTo(StatescriptNodeProperty property)
	{
		var resolver = new EffectDataResolverResource
		{
			IsArray = _isArray,
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

		property.Resolver = resolver;
	}

	/// <inheritdoc/>
	public override bool TryGetInlineSummary(out string summary)
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

			summary = effectCount switch
			{
				0 => "Empty",
				1 => "1 effect",
				_ => $"{effectCount} effects",
			};
			return true;
		}

		summary = _selectedEffect?.Name ?? "(None)";
		return true;
	}

	/// <inheritdoc/>
	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_contentContainer = null;
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

	private void LoadExistingState(EffectDataResolverResource? resolver)
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

	private void RebuildContent()
	{
		if (_contentContainer is null)
		{
			return;
		}

		foreach (Node child in _contentContainer.GetChildren())
		{
			_contentContainer.RemoveChild(child);
			child.Free();
		}

		if (_isArray)
		{
			BuildArrayEditor();
			return;
		}

		_contentContainer.AddChild(CreateEffectPicker(
			_selectedEffect,
			effect =>
			{
				_selectedEffect = effect;
				_onChanged?.Invoke();
			}));
	}

	private void BuildArrayEditor()
	{
		if (_contentContainer is null)
		{
			return;
		}

		var headerRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_contentContainer.AddChild(headerRow);

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
			_contentContainer.AddChild(row);

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
					_onChanged?.Invoke();
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
		RebuildContent();
		_onChanged?.Invoke();
		RaiseLayoutSizeChanged();
	}

	private void OnRemoveEffectPressed(int index)
	{
		if (index < 0 || index >= _selectedEffects.Count)
		{
			return;
		}

		_selectedEffects.RemoveAt(index);
		RebuildContent();
		_onChanged?.Invoke();
		RaiseLayoutSizeChanged();
	}
}
#endif
