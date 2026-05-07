// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using GodotVector2 = Godot.Vector2;
using SysVector2 = System.Numerics.Vector2;
using SysVector3 = System.Numerics.Vector3;
using SysVector4 = System.Numerics.Vector4;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class VectorFromValuesResolverEditor : NodeEditorProperty
{
	private StatescriptGraph? _graph;
	private Action? _onChanged;
	private StatescriptVariableType _valueType = StatescriptVariableType.Vector3;
	private FoldableContainer? _xFoldable;
	private FoldableContainer? _yFoldable;
	private FoldableContainer? _zFoldable;
	private FoldableContainer? _wFoldable;
	private OptionButton? _xResolverDropdown;
	private OptionButton? _yResolverDropdown;
	private OptionButton? _zResolverDropdown;
	private OptionButton? _wResolverDropdown;
	private VBoxContainer? _xEditorContainer;
	private VBoxContainer? _yEditorContainer;
	private VBoxContainer? _zEditorContainer;
	private VBoxContainer? _wEditorContainer;
	private NodeEditorProperty? _xEditor;
	private NodeEditorProperty? _yEditor;
	private NodeEditorProperty? _zEditor;
	private NodeEditorProperty? _wEditor;
	private List<Func<NodeEditorProperty>> _factories = [];

	public override string DisplayName => "Vector From Values";

	public override string ResolverTypeId => "VectorFromValues";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(SysVector2)
			|| expectedType == typeof(SysVector3)
			|| expectedType == typeof(SysVector4);
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
		_valueType = ResolveValueType(property, expectedType);
		_factories = ResolverEditorFactoryCatalog.GetCompatibleFactories(typeof(float), typeof(double));

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		var existing = property?.Resolver as VectorFromValuesResolverResource;
		if (CanEditValueType(expectedType))
		{
			root.AddChild(CreateTypeRow());
		}

		BuildComponentSlot(
			root,
			"X:",
			existing?.X,
			existing?.XFolded ?? true,
			x => _xFoldable = x,
			x => _xResolverDropdown = x,
			x => _xEditorContainer = x,
			x => _xEditor = x);

		BuildComponentSlot(
			root,
			"Y:",
			existing?.Y,
			existing?.YFolded ?? true,
			x => _yFoldable = x,
			x => _yResolverDropdown = x,
			x => _yEditorContainer = x,
			x => _yEditor = x);

		BuildComponentSlot(
			root,
			"Z:",
			existing?.Z,
			existing?.ZFolded ?? true,
			x => _zFoldable = x,
			x => _zResolverDropdown = x,
			x => _zEditorContainer = x,
			x => _zEditor = x);

		BuildComponentSlot(
			root,
			"W:",
			existing?.W,
			existing?.WFolded ?? true,
			x => _wFoldable = x,
			x => _wResolverDropdown = x,
			x => _wEditorContainer = x,
			x => _wEditor = x);

		RefreshVisibleComponents();
		UpdateFoldableTitles();
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new VectorFromValuesResolverResource
		{
			ValueType = _valueType,
			X = SaveNestedEditor(_xEditor),
			Y = SaveNestedEditor(_yEditor),
			Z = SaveNestedEditor(_zEditor),
			W = SaveNestedEditor(_wEditor),
			XFolded = _xFoldable?.Folded ?? false,
			YFolded = _yFoldable?.Folded ?? false,
			ZFolded = _zFoldable?.Folded ?? false,
			WFolded = _wFoldable?.Folded ?? false,
		};
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_xEditor?.ClearCallbacks();
		_yEditor?.ClearCallbacks();
		_zEditor?.ClearCallbacks();
		_wEditor?.ClearCallbacks();
		_xFoldable = null;
		_yFoldable = null;
		_zFoldable = null;
		_wFoldable = null;
		_xResolverDropdown = null;
		_yResolverDropdown = null;
		_zResolverDropdown = null;
		_wResolverDropdown = null;
		_xEditorContainer = null;
		_yEditorContainer = null;
		_zEditorContainer = null;
		_wEditorContainer = null;
		_xEditor = null;
		_yEditor = null;
		_zEditor = null;
		_wEditor = null;
	}

	private static void SetFoldableVisible(FoldableContainer? foldable, bool visible)
	{
		if (foldable is not null)
		{
			foldable.Visible = visible;
		}
	}

	private static StatescriptResolverResource? SaveNestedEditor(NodeEditorProperty? editor)
	{
		if (editor is null)
		{
			return null;
		}

		var property = new StatescriptNodeProperty();
		editor.SaveTo(property);
		return property.Resolver;
	}

	private static bool CanEditValueType(Type expectedType)
	{
		return expectedType != typeof(SysVector2)
			&& expectedType != typeof(SysVector3)
			&& expectedType != typeof(SysVector4);
	}

	private static StatescriptVariableType ResolveValueType(StatescriptNodeProperty? property, Type expectedType)
	{
		if (property?.Resolver is VectorFromValuesResolverResource existing)
		{
			return NormalizeValueType(existing.ValueType);
		}

		if (expectedType == typeof(SysVector2))
		{
			return StatescriptVariableType.Vector2;
		}

		if (expectedType == typeof(SysVector4))
		{
			return StatescriptVariableType.Vector4;
		}

		return StatescriptVariableType.Vector3;
	}

	private static StatescriptVariableType NormalizeValueType(StatescriptVariableType valueType)
	{
		return valueType switch
		{
			StatescriptVariableType.Vector2 => StatescriptVariableType.Vector2,
			StatescriptVariableType.Vector4 => StatescriptVariableType.Vector4,
			_ => StatescriptVariableType.Vector3,
		};
	}

	private HBoxContainer CreateTypeRow()
	{
		var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		row.AddChild(new Label
		{
			Text = "Type:",
			CustomMinimumSize = new GodotVector2(45, 0),
			HorizontalAlignment = HorizontalAlignment.Right,
		});

		var dropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		dropdown.AddItem("Vector2");
		dropdown.AddItem("Vector3");
		dropdown.AddItem("Vector4");
		dropdown.Selected = _valueType switch
		{
			StatescriptVariableType.Vector2 => 0,
			StatescriptVariableType.Vector4 => 2,
			_ => 1,
		};
		dropdown.ItemSelected += OnTypeDropdownItemSelected;
		row.AddChild(dropdown);
		return row;
	}

	private void BuildComponentSlot(
		VBoxContainer root,
		string title,
		StatescriptResolverResource? existingResolver,
		bool folded,
		Action<FoldableContainer> setFoldable,
		Action<OptionButton> setDropdown,
		Action<VBoxContainer> setContainer,
		Action<NodeEditorProperty?> setEditor)
	{
		FoldableContainer foldable = new() { Title = title, Folded = folded };
		foldable.FoldingChanged += _ =>
		{
			UpdateFoldableTitles();
			_onChanged?.Invoke();
			RaiseLayoutSizeChanged();
		};
		setFoldable(foldable);
		root.AddChild(foldable);

		var container = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		foldable.AddChild(container);

		OptionButton resolverDropdown =
			NestedResolverEditorUtilities.CreateResolverDropdownControl(_factories, existingResolver);
		setDropdown(resolverDropdown);
		container.AddChild(resolverDropdown);

		var editorContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		setContainer(editorContainer);
		container.AddChild(editorContainer);

		int selectedIndex = NestedResolverEditorUtilities.GetSelectedIndex(_factories, existingResolver);
		ShowNestedEditor(selectedIndex, existingResolver, editorContainer, setEditor);

		resolverDropdown.ItemSelected += index =>
		{
			NestedResolverEditorUtilities.ClearContainer(editorContainer);
			setEditor(null);
			ShowNestedEditor((int)index, null, editorContainer, setEditor);
			UpdateFoldableTitles();
			_onChanged?.Invoke();
			RaiseLayoutSizeChanged();
		};
	}

	private void ShowNestedEditor(
		int factoryIndex,
		StatescriptResolverResource? existingResolver,
		VBoxContainer container,
		Action<NodeEditorProperty?> setEditor)
	{
		if (_graph is null || factoryIndex < 0 || factoryIndex >= _factories.Count)
		{
			return;
		}

		NodeEditorProperty? editor = NestedResolverEditorUtilities.CreateNestedEditor(
			_graph,
			_factories,
			factoryIndex,
			existingResolver,
			[typeof(float), typeof(double)],
			OnNestedEditorChanged,
			RaiseLayoutSizeChanged);

		if (editor is null)
		{
			return;
		}

		container.AddChild(editor);
		setEditor(editor);
	}

	private void OnTypeDropdownItemSelected(long index)
	{
		_valueType = index switch
		{
			0 => StatescriptVariableType.Vector2,
			2 => StatescriptVariableType.Vector4,
			_ => StatescriptVariableType.Vector3,
		};

		RefreshVisibleComponents();
		UpdateFoldableTitles();
		_onChanged?.Invoke();
		RaiseLayoutSizeChanged();
	}

	private void RefreshVisibleComponents()
	{
		bool showZ = _valueType != StatescriptVariableType.Vector2;
		bool showW = _valueType == StatescriptVariableType.Vector4;

		SetFoldableVisible(_zFoldable, showZ);
		SetFoldableVisible(_wFoldable, showW);
	}

	private void OnNestedEditorChanged()
	{
		UpdateFoldableTitles();
		_onChanged?.Invoke();
	}

	private void UpdateFoldableTitles()
	{
		if (_xFoldable is not null)
		{
			InlineConstantSummaryFormatter.ApplyFoldableTitle("X:", _xFoldable, _xEditor);
		}

		if (_yFoldable is not null)
		{
			InlineConstantSummaryFormatter.ApplyFoldableTitle("Y:", _yFoldable, _yEditor);
		}

		if (_zFoldable is not null)
		{
			InlineConstantSummaryFormatter.ApplyFoldableTitle("Z:", _zFoldable, _zEditor);
		}

		if (_wFoldable is not null)
		{
			InlineConstantSummaryFormatter.ApplyFoldableTitle("W:", _wFoldable, _wEditor);
		}
	}
}
#endif
