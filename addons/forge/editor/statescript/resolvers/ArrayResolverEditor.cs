// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using Godot.Collections;
using GodotVector2 = Godot.Vector2;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class ArrayResolverEditor : NodeEditorProperty
{
	private readonly List<FoldableContainer> _elementFoldables = [];
	private readonly List<NodeEditorProperty?> _elementEditors = [];
	private readonly List<VBoxContainer> _elementEditorContainers = [];
	private readonly List<Func<NodeEditorProperty>> _factories = [];
	private readonly List<StatescriptResolverResource?> _elementResolverResources = [];
	private readonly List<bool> _elementFoldedStates = [];

	private StatescriptGraph? _graph;
	private Action? _onChanged;
	private Type _expectedType = typeof(int);
	private bool _isExpanded;

	private Button? _toggleButton;
	private VBoxContainer? _elementsContainer;

	public override string DisplayName => "Array";

	public override string ResolverTypeId => "Array";

	public override bool SupportsScalarValues => false;

	public override bool SupportsArrayValues => true;

	public override bool IsCompatibleWith(Type expectedType)
	{
		return StatescriptVariableTypeConverter.TryFromSystemType(expectedType, out _);
	}

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

		_factories.Clear();
		_factories.AddRange(ResolverEditorFactoryCatalog.GetCompatibleFactories(expectedType));
		_factories.RemoveAll(factory => StatescriptResolverRegistry.GetResolverTypeId(factory) == ResolverTypeId);

		LoadExistingState(property?.Resolver as ArrayResolverResource);

		CustomMinimumSize = new GodotVector2(220, 40);

		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);
		root.AddChild(CreateArrayEditor());
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		CaptureEditorState();
		StatescriptVariableType elementType = ResolveElementVariableType();

		var resolvers = new Array<StatescriptResolverResource>();
		for (int i = 0; i < _elementResolverResources.Count; i++)
		{
			if (_elementResolverResources[i] is StatescriptResolverResource resolver)
			{
				resolvers.Add(resolver);
			}
		}

		property.Resolver = new ArrayResolverResource
		{
			ElementType = elementType,
			ElementResolvers = resolvers,
			IsExpanded = _isExpanded,
			ElementFoldedStates = [.. _elementFoldedStates],
		};
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		int count = _elementResolverResources.Count;
		summary = count switch
		{
			0 => "Empty",
			1 => "1 item",
			_ => $"{count} items",
		};
		return true;
	}

	public override bool TryGetHighlightedVariableName(out string variableName)
	{
		for (int i = 0; i < _elementEditors.Count; i++)
		{
			if (_elementEditors[i]?.TryGetHighlightedVariableName(out variableName) == true)
			{
				return true;
			}
		}

		variableName = string.Empty;
		return false;
	}

	public override bool TryGetHighlightedSharedVariable(out string sharedVariableSetPath, out string variableName)
	{
		for (int i = 0; i < _elementEditors.Count; i++)
		{
			if (_elementEditors[i]?.TryGetHighlightedSharedVariable(out sharedVariableSetPath, out variableName)
				== true)
			{
				return true;
			}
		}

		sharedVariableSetPath = string.Empty;
		variableName = string.Empty;
		return false;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;

		for (int i = 0; i < _elementEditors.Count; i++)
		{
			_elementEditors[i]?.ClearCallbacks();
		}

		_toggleButton = null;
		_elementsContainer = null;
		_elementFoldables.Clear();
		_elementEditors.Clear();
		_elementEditorContainers.Clear();
	}

	private static string GetElementTitle(int index)
	{
		return $"[{index}]";
	}

	private static StatescriptResolverResource? SaveNestedEditor(NodeEditorProperty editor)
	{
		var property = new StatescriptNodeProperty();
		editor.SaveTo(property);
		return property.Resolver;
	}

	private void LoadExistingState(ArrayResolverResource? existing)
	{
		_elementResolverResources.Clear();
		_elementFoldedStates.Clear();
		_isExpanded = existing?.IsExpanded ?? false;

		if (existing is null)
		{
			return;
		}

		for (int i = 0; i < existing.ElementResolvers.Count; i++)
		{
			_elementResolverResources.Add(existing.ElementResolvers[i]);
			_elementFoldedStates.Add(i < existing.ElementFoldedStates.Count && existing.ElementFoldedStates[i]);
		}
	}

	private VBoxContainer CreateArrayEditor()
	{
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

		var headerRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		root.AddChild(headerRow);

		_toggleButton = new Button
		{
			Text = GetHeaderText(),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			ToggleMode = true,
			ButtonPressed = _isExpanded,
		};
		_toggleButton.Toggled += OnExpandedToggled;
		headerRow.AddChild(_toggleButton);

		Texture2D addIcon = EditorInterface.Singleton.GetEditorTheme().GetIcon("Add", "EditorIcons");
		var addButton = new Button
		{
			Icon = addIcon,
			Flat = true,
			TooltipText = "Add Element",
			Disabled = _factories.Count == 0,
			CustomMinimumSize = new GodotVector2(24, 24),
		};
		addButton.Pressed += OnAddElementPressed;
		headerRow.AddChild(addButton);

		if (_factories.Count == 0)
		{
			root.AddChild(new Label
			{
				Text = "No compatible element resolvers.",
				Modulate = Colors.IndianRed,
			});
			return root;
		}

		_elementsContainer = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			Visible = _isExpanded,
		};
		root.AddChild(_elementsContainer);

		RebuildElementRows();
		return root;
	}

	private void OnExpandedToggled(bool expanded)
	{
		_isExpanded = expanded;

		if (_elementsContainer is not null)
		{
			_elementsContainer.Visible = expanded;
		}

		_onChanged?.Invoke();
		RaiseLayoutSizeChanged();
	}

	private void OnAddElementPressed()
	{
		CaptureEditorState();

		StatescriptResolverResource? resolver = CreateResolverResourceForFactory(GetDefaultElementFactoryIndex());
		if (resolver is null)
		{
			return;
		}

		_elementResolverResources.Add(resolver);
		_elementFoldedStates.Add(false);
		_isExpanded = true;

		if (_elementsContainer is not null)
		{
			_elementsContainer.Visible = true;
		}

		RebuildElementRows();
		_onChanged?.Invoke();
		RaiseLayoutSizeChanged();
	}

	private void RebuildElementRows()
	{
		if (_elementsContainer is null)
		{
			return;
		}

		NestedResolverEditorUtilities.ClearContainer(_elementsContainer);
		_elementFoldables.Clear();
		_elementEditors.Clear();
		_elementEditorContainers.Clear();

		Texture2D removeIcon = EditorInterface.Singleton.GetEditorTheme().GetIcon("Remove", "EditorIcons");

		for (int i = 0; i < _elementResolverResources.Count; i++)
		{
			int capturedIndex = i;
			var foldable = new FoldableContainer
			{
				Title = GetElementTitle(i),
				Folded = i < _elementFoldedStates.Count && _elementFoldedStates[i],
			};
			foldable.FoldingChanged += _ =>
			{
				OnElementFoldStateChanged(capturedIndex);
			};

			_elementFoldables.Add(foldable);
			_elementsContainer.AddChild(foldable);

			var content = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			foldable.AddChild(content);

			OptionButton dropdown = NestedResolverEditorUtilities.CreateResolverDropdownControl(
				_factories,
				_elementResolverResources[i]);
			if (_elementResolverResources[i] is null)
			{
				dropdown.Selected = GetDefaultElementFactoryIndex();
			}

			HBoxContainer selectorRow = NestedResolverEditorUtilities.CreateResolverSelectorRow(dropdown);

			var removeButton = new Button
			{
				Icon = removeIcon,
				Flat = true,
				TooltipText = "Remove Element",
				CustomMinimumSize = new GodotVector2(24, 24),
			};
			removeButton.Pressed += () => OnRemoveElementPressed(capturedIndex);
			selectorRow.AddChild(removeButton);
			content.AddChild(selectorRow);

			var editorContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			content.AddChild(editorContainer);
			_elementEditorContainers.Add(editorContainer);

			int selectedFactoryIndex = _elementResolverResources[i] is null
				? GetDefaultElementFactoryIndex()
				: NestedResolverEditorUtilities.GetSelectedIndex(_factories, _elementResolverResources[i]);

			NodeEditorProperty? editor = NestedResolverEditorUtilities.CreateNestedEditor(
				_graph,
				_factories,
				selectedFactoryIndex,
				_elementResolverResources[i],
				[_expectedType],
				() => OnElementEditorChanged(capturedIndex),
				RaiseLayoutSizeChanged);
			_elementEditors.Add(editor);

			if (editor is not null)
			{
				editorContainer.AddChild(editor);
			}

			dropdown.ItemSelected += selectedIndex => OnElementResolverChanged(capturedIndex, (int)selectedIndex);
			UpdateElementFoldableTitle(i);
		}

		UpdateHeaderText();
	}

	private void OnElementResolverChanged(int elementIndex, int factoryIndex)
	{
		CaptureEditorState();

		StatescriptResolverResource? resolver = CreateResolverResourceForFactory(factoryIndex);
		if (resolver is null)
		{
			return;
		}

		_elementResolverResources[elementIndex] = resolver;
		RebuildElementRows();
		_onChanged?.Invoke();
		RaiseLayoutSizeChanged();
	}

	private void OnRemoveElementPressed(int elementIndex)
	{
		CaptureEditorState();
		_elementResolverResources.RemoveAt(elementIndex);
		_elementFoldedStates.RemoveAt(elementIndex);
		RebuildElementRows();
		_onChanged?.Invoke();
		RaiseLayoutSizeChanged();
	}

	private void OnElementFoldStateChanged(int elementIndex)
	{
		if (elementIndex < 0 || elementIndex >= _elementFoldables.Count)
		{
			return;
		}

		while (_elementFoldedStates.Count <= elementIndex)
		{
			_elementFoldedStates.Add(false);
		}

		_elementFoldedStates[elementIndex] = _elementFoldables[elementIndex].Folded;
		UpdateElementFoldableTitle(elementIndex);
		_onChanged?.Invoke();
		RaiseLayoutSizeChanged();
	}

	private void OnElementEditorChanged(int elementIndex)
	{
		UpdateElementFoldableTitle(elementIndex);
		_onChanged?.Invoke();
	}

	private void CaptureEditorState()
	{
		for (int i = 0; i < _elementEditors.Count; i++)
		{
			while (_elementResolverResources.Count <= i)
			{
				_elementResolverResources.Add(null);
			}

			while (_elementFoldedStates.Count <= i)
			{
				_elementFoldedStates.Add(false);
			}

			if (_elementEditors[i] is NodeEditorProperty editor)
			{
				_elementResolverResources[i] = SaveNestedEditor(editor);
			}

			if (i < _elementFoldables.Count)
			{
				_elementFoldedStates[i] = _elementFoldables[i].Folded;
			}
		}
	}

	private void UpdateElementFoldableTitle(int elementIndex)
	{
		if (elementIndex < 0 || elementIndex >= _elementFoldables.Count)
		{
			return;
		}

		InlineConstantSummaryFormatter.ApplyFoldableTitle(
			GetElementTitle(elementIndex),
			_elementFoldables[elementIndex],
			elementIndex < _elementEditors.Count ? _elementEditors[elementIndex] : null);
	}

	private void UpdateHeaderText()
	{
		if (_toggleButton is not null)
		{
			_toggleButton.Text = GetHeaderText();
		}
	}

	private string GetHeaderText()
	{
		return $"Array (size {_elementResolverResources.Count})";
	}

	private int GetDefaultElementFactoryIndex()
	{
		return StatescriptResolverRegistry.GetDefaultFactoryIndex(_factories, _expectedType, false);
	}

	private StatescriptVariableType ResolveElementVariableType()
	{
		return StatescriptVariableTypeConverter.TryFromSystemType(
			_expectedType,
			out StatescriptVariableType elementType)
				? elementType
				: StatescriptVariableType.Int;
	}

	private StatescriptResolverResource? CreateResolverResourceForFactory(int factoryIndex)
	{
		if (_graph is null || factoryIndex < 0 || factoryIndex >= _factories.Count)
		{
			return null;
		}

		NodeEditorProperty editor = _factories[factoryIndex]();

		try
		{
			editor.ConfigureAllowedExpectedTypes(_expectedType);
			editor.Setup(_graph, null, _expectedType, static () => { }, false);
			var property = new StatescriptNodeProperty();
			editor.SaveTo(property);
			return property.Resolver;
		}
		finally
		{
			editor.ClearCallbacks();

			if (IsInstanceValid(editor))
			{
				editor.Free();
			}
		}
	}
}
#endif
