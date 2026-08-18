// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that authors a constant <see cref="PackedScene"/> reference (scalar or array).
/// </summary>
[Tool]
internal sealed partial class ScenePickerResolverEditor : NodeEditorProperty
{
	private readonly List<PackedScene?> _selectedScenes = [];

	private Action? _onChanged;
	private bool _isArray;
	private PackedScene? _selectedScene;
	private VBoxContainer? _sceneContainer;

	public override string DisplayName => "Scene";

	public override string ResolverTypeId => "ScenePicker";

	public override bool SupportsArrayValues => false;

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(PackedScene);
	}

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		_onChanged = onChanged;
		_isArray = isArray;

		var existingResource = property?.Resolver as ScenePickerResolverResource;
		LoadExistingState(existingResource);

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		_sceneContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		root.AddChild(_sceneContainer);

		RebuildSceneContent();
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		var resolver = new ScenePickerResolverResource { IsArray = _isArray };

		if (_isArray)
		{
			var scenes = new Array<PackedScene>();

			for (int i = 0; i < _selectedScenes.Count; i++)
			{
				if (_selectedScenes[i] is PackedScene scene)
				{
					scenes.Add(scene);
				}
			}

			resolver.Scenes = scenes;
		}
		else
		{
			resolver.Scene = _selectedScene;
		}

		property.Resolver = resolver;
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = GetSceneSummary();
		return true;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_sceneContainer = null;
	}

	private static EditorResourcePicker CreateScenePicker(PackedScene? scene, Action<PackedScene?> onChanged)
	{
		var picker = new EditorResourcePicker
		{
			BaseType = nameof(PackedScene),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			EditedResource = scene,
		};

		picker.ResourceChanged += resource => onChanged(resource as PackedScene);
		return picker;
	}

	private static string DescribeScene(PackedScene? scene)
	{
		if (scene is null)
		{
			return "(None)";
		}

		return string.IsNullOrEmpty(scene.ResourcePath)
			? "Scene"
			: System.IO.Path.GetFileNameWithoutExtension(scene.ResourcePath);
	}

	private void LoadExistingState(ScenePickerResolverResource? resolver)
	{
		_selectedScenes.Clear();
		_selectedScene = null;

		if (resolver is null)
		{
			return;
		}

		if (_isArray)
		{
			for (int i = 0; i < resolver.Scenes.Count; i++)
			{
				_selectedScenes.Add(resolver.Scenes[i]);
			}

			return;
		}

		_selectedScene = resolver.Scene;
	}

	private string GetSceneSummary()
	{
		if (!_isArray)
		{
			return DescribeScene(_selectedScene);
		}

		int sceneCount = 0;

		for (int i = 0; i < _selectedScenes.Count; i++)
		{
			if (_selectedScenes[i] is not null)
			{
				sceneCount++;
			}
		}

		return sceneCount switch
		{
			0 => "Empty",
			1 => "1 scene",
			_ => $"{sceneCount} scenes",
		};
	}

	private void RebuildSceneContent()
	{
		if (_sceneContainer is null)
		{
			return;
		}

		foreach (Node child in _sceneContainer.GetChildren())
		{
			_sceneContainer.RemoveChild(child);
			child.QueueFree();
		}

		if (_isArray)
		{
			BuildArrayEditor();
			return;
		}

		_sceneContainer.AddChild(CreateScenePicker(
			_selectedScene,
			scene =>
			{
				_selectedScene = scene;
				_onChanged?.Invoke();
			}));
	}

	private void BuildArrayEditor()
	{
		if (_sceneContainer is null)
		{
			return;
		}

		var headerRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_sceneContainer.AddChild(headerRow);

		headerRow.AddChild(new Label
		{
			Text = "Scenes",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		});

		var addButton = new Button
		{
			Text = "Add",
			SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
		};
		addButton.Pressed += OnAddScenePressed;
		headerRow.AddChild(addButton);

		for (int i = 0; i < _selectedScenes.Count; i++)
		{
			int capturedIndex = i;
			var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			_sceneContainer.AddChild(row);

			row.AddChild(new Label
			{
				Text = $"[{i}]",
				CustomMinimumSize = new Vector2(28, 0),
				HorizontalAlignment = HorizontalAlignment.Right,
			});

			row.AddChild(CreateScenePicker(
				_selectedScenes[capturedIndex],
				scene =>
				{
					_selectedScenes[capturedIndex] = scene;
					_onChanged?.Invoke();
				}));

			var removeButton = new Button
			{
				Text = "Remove",
				SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
			};
			removeButton.Pressed += () => OnRemoveScenePressed(capturedIndex);
			row.AddChild(removeButton);
		}
	}

	private void OnAddScenePressed()
	{
		_selectedScenes.Add(null);
		RebuildSceneContent();
		_onChanged?.Invoke();
		RaiseLayoutSizeChanged();
	}

	private void OnRemoveScenePressed(int index)
	{
		if (index < 0 || index >= _selectedScenes.Count)
		{
			return;
		}

		_selectedScenes.RemoveAt(index);
		RebuildSceneContent();
		_onChanged?.Invoke();
		RaiseLayoutSizeChanged();
	}
}
#endif
